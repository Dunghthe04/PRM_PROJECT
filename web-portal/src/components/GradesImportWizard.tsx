import { DownloadOutlined, InboxOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Card,
  Select,
  Space,
  Steps,
  Typography,
  Upload,
  message,
} from 'antd'
import { useEffect, useMemo, useState } from 'react'
import { api, downloadBlob, uploadExcel } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type {
  ClassItem,
  ExcelImportResult,
  SemesterItem,
  SubjectItem,
  TeacherClass,
} from '../types'
import { pickDefaultHomeroomClassId } from '../utils/teacherClasses'

type Mode = 'teacher' | 'admin'

/**
 * Wizard nhập điểm Excel:
 * 1) Chọn kỳ → 2) Chọn lớp → 3) Chọn môn (GV: chỉ môn được phân công) → tải mẫu / upload.
 */
export function GradesImportWizard({ mode }: { mode: Mode }) {
  const { user } = useAuth()
  const [semesters, setSemesters] = useState<SemesterItem[]>([])
  const [teacherClasses, setTeacherClasses] = useState<TeacherClass[]>([])
  const [allClasses, setAllClasses] = useState<ClassItem[]>([])
  const [allSubjects, setAllSubjects] = useState<SubjectItem[]>([])

  const [semesterId, setSemesterId] = useState<number | undefined>()
  const [classId, setClassId] = useState<number | undefined>()
  const [subjectId, setSubjectId] = useState<number | undefined>()
  const [result, setResult] = useState<ExcelImportResult | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    void (async () => {
      try {
        const semRes = await api.get<SemesterItem[]>('/semesters')
        setSemesters(semRes.data)

        if (mode === 'teacher' && user) {
          const clsRes = await api.get<TeacherClass[]>(
            `/teachers/${user.id}/classes`,
          )
          setTeacherClasses(clsRes.data)
          // Gợi ý kỳ đang diễn ra / lớp đầu tiên
          const defaultClassId = pickDefaultHomeroomClassId(
            clsRes.data.map((c) => ({ ...c, isHomeroom: true })),
            semRes.data,
          )
          // pickDefault chỉ homeroom — với import điểm dùng mọi lớp được phân công
          const bySem = new Map<number, TeacherClass>()
          for (const c of clsRes.data) {
            if (!bySem.has(c.classId)) bySem.set(c.classId, c)
          }
          const unique = [...bySem.values()]
          const todayMatch = unique.find((c) => {
            const sem = semRes.data.find((s) => s.id === c.semesterId)
            if (!sem) return false
            const t = Date.now()
            return (
              new Date(sem.startDate).getTime() <= t &&
              t <= new Date(sem.endDate).getTime()
            )
          })
          const pick = todayMatch ?? unique[0]
          if (pick) {
            setSemesterId(pick.semesterId)
            setClassId(pick.classId)
          } else if (defaultClassId) {
            const c = clsRes.data.find((x) => x.classId === defaultClassId)
            if (c) {
              setSemesterId(c.semesterId)
              setClassId(c.classId)
            }
          }
        } else {
          const [c, s] = await Promise.all([
            api.get<ClassItem[]>('/classes'),
            api.get<SubjectItem[]>('/subjects'),
          ])
          setAllClasses(c.data)
          setAllSubjects(s.data)
          const today = Date.now()
          const active = semRes.data.find(
            (sem) =>
              new Date(sem.startDate).getTime() <= today &&
              today <= new Date(sem.endDate).getTime(),
          )
          if (active) setSemesterId(active.id)
        }
      } catch {
        message.error('Không tải được danh mục lớp/môn.')
      }
    })()
  }, [mode, user])

  const classOptions = useMemo(() => {
    if (mode === 'teacher') {
      const map = new Map<number, TeacherClass>()
      for (const c of teacherClasses) {
        if (semesterId && c.semesterId !== semesterId) continue
        if (!map.has(c.classId)) map.set(c.classId, c)
      }
      return [...map.values()].map((c) => ({
        value: c.classId,
        label: `${c.className} · ${c.semesterName ?? ''}`,
      }))
    }
    return allClasses
      .filter((c) => !semesterId || c.semesterId === semesterId)
      .map((c) => ({
        value: c.id,
        label: `${c.name} · ${c.semesterName ?? ''}`,
      }))
  }, [mode, teacherClasses, allClasses, semesterId])

  const subjectOptions = useMemo(() => {
    if (!classId) return []
    if (mode === 'teacher') {
      return teacherClasses
        .filter((c) => c.classId === classId)
        .map((c) => ({
          value: c.subjectId,
          label: `${c.subjectName} (${c.subjectCode})`,
        }))
    }
    return allSubjects.map((s) => ({
      value: s.id,
      label: `${s.name} (${s.code})`,
    }))
  }, [mode, teacherClasses, allSubjects, classId])

  const ready = classId != null && subjectId != null
  const step = !semesterId ? 0 : !classId ? 1 : !subjectId ? 2 : 3

  const qs = ready ? `?classId=${classId}&subjectId=${subjectId}` : ''

  return (
    <Card title="Nhập điểm bằng Excel">
      <Steps
        size="small"
        current={step}
        style={{ marginBottom: 24 }}
        items={[
          { title: 'Chọn kỳ' },
          { title: 'Chọn lớp' },
          { title: 'Chọn môn' },
          { title: 'Import Excel' },
        ]}
      />

      <Space wrap style={{ marginBottom: 16 }}>
        <Select
          style={{ minWidth: 240 }}
          placeholder="Học kỳ"
          value={semesterId}
          options={semesters.map((s) => ({ value: s.id, label: s.name }))}
          onChange={(v) => {
            setSemesterId(v)
            setClassId(undefined)
            setSubjectId(undefined)
            setResult(null)
          }}
        />
        <Select
          style={{ minWidth: 240 }}
          placeholder="Lớp"
          value={classId}
          options={classOptions}
          disabled={!semesterId}
          onChange={(v) => {
            setClassId(v)
            setSubjectId(undefined)
            setResult(null)
          }}
        />
        <Select
          style={{ minWidth: 220 }}
          placeholder="Môn học"
          value={subjectId}
          options={subjectOptions}
          disabled={!classId}
          onChange={(v) => {
            setSubjectId(v)
            setResult(null)
          }}
        />
      </Space>

      <Typography.Paragraph type="secondary">
        File mẫu theo sổ điểm THPT: <b>Miệng 1–3</b>, <b>15 phút 1–3</b>,{' '}
        <b>1 tiết 1–2</b>, <b>Giữa kỳ</b>, <b>Cuối kỳ</b> — đã có sẵn danh sách HS.
        Ô trống = chưa nhập (bỏ qua). Điểm import được <b>công bố ngay</b>.
        {mode === 'teacher' &&
          ' Bạn chỉ thấy môn được phân công giảng dạy.'}
      </Typography.Paragraph>

      <Space style={{ marginBottom: 16 }}>
        <Button
          icon={<DownloadOutlined />}
          disabled={!ready}
          onClick={async () => {
            try {
              await downloadBlob(
                `/import/grades/template${qs}`,
                'Diem.xlsx',
              )
              message.success('Đã tải file mẫu.')
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Không tải được mẫu.'
              message.error(msg)
            }
          }}
        >
          1. Tải file mẫu Excel
        </Button>
      </Space>

      <Upload.Dragger
        accept=".xlsx"
        maxCount={1}
        showUploadList={false}
        disabled={!ready || loading}
        customRequest={async ({ file, onSuccess, onError }) => {
          setLoading(true)
          setResult(null)
          try {
            const data = (await uploadExcel(
              `/import/grades${qs}`,
              file as File,
            )) as ExcelImportResult
            setResult(data)
            message.success(data.message || 'Import xong.')
            onSuccess?.(data)
          } catch (e: unknown) {
            const msg =
              (e as { response?: { data?: { message?: string } } })?.response
                ?.data?.message ?? 'Import thất bại.'
            message.error(msg)
            onError?.(e as Error)
          } finally {
            setLoading(false)
          }
        }}
      >
        <p className="ant-upload-drag-icon">
          <InboxOutlined />
        </p>
        <p className="ant-upload-text">
          {ready
            ? '2. Kéo thả hoặc chọn file .xlsx đã điền điểm'
            : 'Chọn kỳ → lớp → môn trước khi upload'}
        </p>
      </Upload.Dragger>

      {result && (
        <Alert
          style={{ marginTop: 16 }}
          type={result.errorCount > 0 ? 'warning' : 'success'}
          message={result.message}
          description={
            result.errors?.length > 0 ? (
              <ul style={{ margin: '8px 0 0', paddingLeft: 18 }}>
                {result.errors.slice(0, 20).map((err) => (
                  <li key={err}>{err}</li>
                ))}
              </ul>
            ) : undefined
          }
        />
      )}
    </Card>
  )
}
