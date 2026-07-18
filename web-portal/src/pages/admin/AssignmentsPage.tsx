import { PlusOutlined } from '@ant-design/icons'
import { Button, Form, Modal, Select, Table, message } from 'antd'
import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/client'
import type {
  ClassItem,
  PagedResult,
  SubjectItem,
  TeacherAssignment,
  User,
} from '../../types'

export function AdminAssignmentsPage() {
  const [items, setItems] = useState<TeacherAssignment[]>([])
  const [teachers, setTeachers] = useState<User[]>([])
  const [classes, setClasses] = useState<ClassItem[]>([])
  const [subjects, setSubjects] = useState<SubjectItem[]>([])
  const [open, setOpen] = useState(false)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    const [a, t, c, s] = await Promise.all([
      api.get<TeacherAssignment[]>('/teacher-assignments'),
      api.get<PagedResult<User>>('/users', {
        params: { page: 1, pageSize: 100, role: 'Teacher' },
      }),
      api.get<ClassItem[]>('/classes'),
      api.get<SubjectItem[]>('/subjects'),
    ])
    setItems(a.data)
    setTeachers(t.data.items)
    setClasses(c.data)
    setSubjects(s.data)
  }, [])

  useEffect(() => {
    void load().catch(() => message.error('Không tải phân công.'))
  }, [load])

  return (
    <>
      <Button
        type="primary"
        icon={<PlusOutlined />}
        style={{ marginBottom: 12, background: '#FF6B00' }}
        onClick={() => {
          form.resetFields()
          setOpen(true)
        }}
      >
        Gán GV — Lớp — Môn
      </Button>
      <Table
        rowKey="id"
        dataSource={items}
        columns={[
          { title: 'Giáo viên', dataIndex: 'teacherName' },
          { title: 'SĐT', dataIndex: 'teacherPhone' },
          { title: 'Lớp', dataIndex: 'className' },
          { title: 'Môn', dataIndex: 'subjectName' },
          { title: 'Mã môn', dataIndex: 'subjectCode' },
          {
            title: '',
            render: (_, row) => (
              <Button
                danger
                size="small"
                onClick={async () => {
                  await api.delete(`/teacher-assignments/${row.id}`)
                  message.success('Đã gỡ.')
                  void load()
                }}
              >
                Xóa
              </Button>
            ),
          },
        ]}
      />
      <Modal
        title="Phân công giảng dạy"
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        okText="Lưu"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={async (v) => {
            try {
              await api.post('/teacher-assignments', v)
              message.success('Đã gán.')
              setOpen(false)
              void load()
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Lỗi.'
              message.error(msg)
            }
          }}
        >
          <Form.Item name="teacherId" label="Giáo viên" rules={[{ required: true }]}>
            <Select
              options={teachers.map((t) => ({
                value: t.id,
                label: `${t.fullName} (${t.phone})`,
              }))}
            />
          </Form.Item>
          <Form.Item name="classId" label="Lớp" rules={[{ required: true }]}>
            <Select
              options={classes.map((c) => ({ value: c.id, label: c.name }))}
            />
          </Form.Item>
          <Form.Item name="subjectId" label="Môn" rules={[{ required: true }]}>
            <Select
              options={subjects.map((s) => ({
                value: s.id,
                label: `${s.code} — ${s.name}`,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
