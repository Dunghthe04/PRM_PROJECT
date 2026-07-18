import { Button, Form, Input, Select, Table, Tabs, message } from 'antd'
import dayjs from 'dayjs'
import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Announcement, PagedResult, User } from '../../types'

export function AdminAnnouncePage() {
  const [mine, setMine] = useState<Announcement[]>([])
  const [teachers, setTeachers] = useState<User[]>([])
  const [form] = Form.useForm()
  const [type, setType] = useState<string>('Global')

  const load = useCallback(async () => {
    const [m, t] = await Promise.all([
      api.get<Announcement[]>('/announcements/mine'),
      api.get<PagedResult<User>>('/users', {
        params: { page: 1, pageSize: 100, role: 'Teacher' },
      }),
    ])
    setMine(m.data)
    setTeachers(t.data.items)
  }, [])

  useEffect(() => {
    void load().catch(() => message.error('Không tải thông báo.'))
  }, [load])

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
              initialValues={{ type: 'Global', sendPush: true }}
              onValuesChange={(_, all) => setType(all.type)}
              onFinish={async (v) => {
                try {
                  await api.post('/announcements', {
                    title: v.title,
                    content: v.content,
                    type: v.type,
                    targetUserId: v.targetUserId,
                    sendPush: true,
                  })
                  message.success('Đã gửi.')
                  form.resetFields()
                  form.setFieldsValue({ type: 'Global' })
                  setType('Global')
                  void load()
                } catch (e: unknown) {
                  const msg =
                    (e as { response?: { data?: { message?: string } } })
                      ?.response?.data?.message ?? 'Gửi thất bại.'
                  message.error(msg)
                }
              }}
            >
              <Form.Item name="type" label="Đối tượng" rules={[{ required: true }]}>
                <Select
                  options={[
                    { value: 'Global', label: 'Toàn trường (Bảng tin)' },
                    { value: 'Teachers', label: 'Toàn bộ giáo viên' },
                    { value: 'Teacher', label: 'Một giáo viên' },
                  ]}
                />
              </Form.Item>
              {type === 'Teacher' && (
                <Form.Item
                  name="targetUserId"
                  label="Giáo viên nhận"
                  rules={[{ required: true }]}
                >
                  <Select
                    options={teachers.map((t) => ({
                      value: t.id,
                      label: `${t.fullName} (${t.phone})`,
                    }))}
                  />
                </Form.Item>
              )}
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
                { title: 'Loại', dataIndex: 'type' },
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
