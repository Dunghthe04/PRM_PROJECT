import { Button, Form, Input, Select, Table, Tabs, message } from 'antd'
import dayjs from 'dayjs'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import type { Announcement, TeacherClass } from '../../types'

export function TeacherAnnouncePage() {
  const { user } = useAuth()
  const [classes, setClasses] = useState<TeacherClass[]>([])
  const [mine, setMine] = useState<Announcement[]>([])
  const [form] = Form.useForm()
  const [selectedClassId, setSelectedClassId] = useState<number | undefined>()

  useEffect(() => {
    if (!user) return
    void api.get<TeacherClass[]>(`/teachers/${user.id}/classes`).then((r) => {
      setClasses(r.data)
    })
  }, [user])

  const classOptions = useMemo(() => {
    const map = new Map<number, TeacherClass>()
    for (const c of classes) {
      const prev = map.get(c.classId)
      if (!prev || c.isHomeroom) map.set(c.classId, c)
    }
    return [...map.values()]
      .sort((a, b) =>
        (a.semesterName ?? '').localeCompare(b.semesterName ?? '', 'vi'),
      )
      .map((c) => ({
        value: c.classId,
        label: `${c.className} · ${c.semesterName ?? '—'}${c.isHomeroom ? ' (CN)' : ''}`,
      }))
  }, [classes])

  const subjectOptions = useMemo(() => {
    if (!selectedClassId) return []
    return classes
      .filter((c) => c.classId === selectedClassId)
      .map((c) => ({
        value: c.subjectId,
        label: c.isHomeroom
          ? `${c.subjectName} (có thể gửi tin CN)`
          : `${c.subjectName} — Bộ môn`,
      }))
  }, [classes, selectedClassId])

  const loadMine = useCallback(async () => {
    const { data } = await api.get<Announcement[]>('/announcements/mine')
    setMine(data)
  }, [])

  useEffect(() => {
    void loadMine()
  }, [loadMine])

  return (
    <Tabs
      items={[
        {
          key: 'send',
          label: 'Soạn gửi',
          children: (
            <Form
              form={form}
              layout="vertical"
              style={{ maxWidth: 640 }}
              initialValues={{ type: 'Class', sendPush: true }}
              onFinish={async (v) => {
                try {
                  await api.post('/announcements', {
                    title: v.title,
                    content: v.content,
                    type: 'Class',
                    targetClassId: v.targetClassId,
                    subjectId: v.subjectId,
                    sendPush: true,
                  })
                  message.success('Đã gửi.')
                  form.resetFields()
                  form.setFieldsValue({ type: 'Class' })
                  void loadMine()
                } catch (e: unknown) {
                  const msg =
                    (e as { response?: { data?: { message?: string } } })
                      ?.response?.data?.message ?? 'Gửi thất bại.'
                  message.error(msg)
                }
              }}
            >
              <Form.Item
                name="targetClassId"
                label="Lớp"
                rules={[{ required: true }]}
              >
                <Select
                  options={classOptions}
                  onChange={(v) => {
                    setSelectedClassId(v)
                    form.setFieldValue('subjectId', undefined)
                  }}
                />
              </Form.Item>
              <Form.Item name="subjectId" label="Môn / phạm vi">
                <Select allowClear options={subjectOptions} />
              </Form.Item>
              <Form.Item name="title" label="Tiêu đề" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item
                name="content"
                label="Nội dung"
                rules={[{ required: true }]}
              >
                <Input.TextArea rows={5} />
              </Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                style={{ background: '#FF6B00' }}
              >
                Gửi
              </Button>
            </Form>
          ),
        },
        {
          key: 'mine',
          label: 'Đã gửi',
          children: (
            <Table
              rowKey="id"
              dataSource={mine}
              columns={[
                { title: 'Tiêu đề', dataIndex: 'title' },
                { title: 'Lớp', dataIndex: 'targetClassName' },
                { title: 'Môn', dataIndex: 'subjectName' },
                {
                  title: 'Thời gian',
                  dataIndex: 'createdAt',
                  render: (d: string) => dayjs(d).format('DD/MM/YYYY HH:mm'),
                },
              ]}
            />
          ),
        },
      ]}
    />
  )
}
