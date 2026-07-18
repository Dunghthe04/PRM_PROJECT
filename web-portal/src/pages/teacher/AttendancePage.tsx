import { Button, DatePicker, Radio, Select, Space, Table, message } from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import type {
  AttendanceRecord,
  ClassStudent,
  SemesterItem,
  TeacherClass,
} from '../../types'
import {
  homeroomSelectOptions,
  pickDefaultHomeroomClassId,
} from '../../utils/teacherClasses'

type Row = {
  studentId: number
  fullName: string
  phone: string
  status: 'Present' | 'Absent' | 'Late'
}

export function TeacherAttendancePage() {
  const { user } = useAuth()
  const [classes, setClasses] = useState<TeacherClass[]>([])
  const [classId, setClassId] = useState<number | undefined>()
  const [date, setDate] = useState<Dayjs>(dayjs())
  const [rows, setRows] = useState<Row[]>([])
  const [saving, setSaving] = useState(false)

  const options = useMemo(() => homeroomSelectOptions(classes), [classes])

  useEffect(() => {
    if (!user) return
    void (async () => {
      try {
        const [clsRes, semRes] = await Promise.all([
          api.get<TeacherClass[]>(`/teachers/${user.id}/classes`),
          api.get<SemesterItem[]>('/semesters'),
        ])
        setClasses(clsRes.data)
        setClassId(pickDefaultHomeroomClassId(clsRes.data, semRes.data))
      } catch {
        message.error('Không tải danh sách lớp.')
      }
    })()
  }, [user])

  const load = useCallback(async () => {
    if (!classId) {
      setRows([])
      return
    }
    const [studentsRes, attRes] = await Promise.all([
      api.get<ClassStudent[]>(`/classes/${classId}/students`),
      api.get<AttendanceRecord[]>('/attendance', {
        params: { classId, date: date.format('YYYY-MM-DD') },
      }),
    ])
    const byStudent = new Map(
      attRes.data.map((a) => [a.studentId, a.status as Row['status']]),
    )
    setRows(
      studentsRes.data.map((s) => ({
        studentId: s.studentId,
        fullName: s.fullName,
        phone: s.phone,
        status: byStudent.get(s.studentId) ?? 'Present',
      })),
    )
  }, [classId, date])

  useEffect(() => {
    void load().catch(() => message.error('Không tải điểm danh.'))
  }, [load])

  return (
    <>
      <Space wrap style={{ marginBottom: 16 }}>
        <Select
          style={{ minWidth: 280 }}
          placeholder="Lớp chủ nhiệm (theo kỳ)"
          value={classId}
          options={options}
          onChange={setClassId}
        />
        <DatePicker value={date} onChange={(d) => d && setDate(d)} />
        <Button
          type="primary"
          loading={saving}
          style={{ background: '#FF6B00' }}
          disabled={!classId || rows.length === 0}
          onClick={async () => {
            if (!classId) return
            setSaving(true)
            try {
              await api.post('/attendance/batch', {
                classId,
                date: date.format('YYYY-MM-DD'),
                entries: rows.map((r) => ({
                  studentId: r.studentId,
                  status: r.status,
                })),
              })
              message.success('Đã lưu điểm danh.')
              void load()
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Lưu thất bại.'
              message.error(msg)
            } finally {
              setSaving(false)
            }
          }}
        >
          Lưu điểm danh
        </Button>
      </Space>
      <Table
        rowKey="studentId"
        dataSource={rows}
        pagination={false}
        columns={[
          { title: 'Họ tên', dataIndex: 'fullName' },
          { title: 'SĐT', dataIndex: 'phone' },
          {
            title: 'Trạng thái',
            dataIndex: 'status',
            render: (status: Row['status'], row) => (
              <Radio.Group
                value={status}
                onChange={(e) =>
                  setRows((prev) =>
                    prev.map((r) =>
                      r.studentId === row.studentId
                        ? { ...r, status: e.target.value }
                        : r,
                    ),
                  )
                }
                optionType="button"
                buttonStyle="solid"
                options={[
                  { value: 'Present', label: 'P' },
                  { value: 'Absent', label: 'A' },
                  { value: 'Late', label: 'L' },
                ]}
              />
            ),
          },
        ]}
      />
    </>
  )
}
