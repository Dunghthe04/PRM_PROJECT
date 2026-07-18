import { PlusOutlined } from '@ant-design/icons'
import {
  Button,
  DatePicker,
  Form,
  Input,
  Modal,
  Select,
  Table,
  Tabs,
  message,
} from 'antd'
import dayjs from 'dayjs'
import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/client'
import type {
  ClassItem,
  PagedResult,
  SemesterItem,
  SubjectItem,
  User,
} from '../../types'

export function AdminCatalogPage() {
  const [semesters, setSemesters] = useState<SemesterItem[]>([])
  const [subjects, setSubjects] = useState<SubjectItem[]>([])
  const [classes, setClasses] = useState<ClassItem[]>([])
  const [teachers, setTeachers] = useState<User[]>([])
  const [open, setOpen] = useState<'sem' | 'sub' | 'cls' | null>(null)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    const [s, sub, c, t] = await Promise.all([
      api.get<SemesterItem[]>('/semesters'),
      api.get<SubjectItem[]>('/subjects'),
      api.get<ClassItem[]>('/classes'),
      api.get<PagedResult<User>>('/users', {
        params: { page: 1, pageSize: 100, role: 'Teacher' },
      }),
    ])
    setSemesters(s.data)
    setSubjects(sub.data)
    setClasses(c.data)
    setTeachers(t.data.items)
  }, [])

  useEffect(() => {
    void load().catch(() => message.error('Không tải danh mục.'))
  }, [load])

  return (
    <>
      <Tabs
        items={[
          {
            key: 'sem',
            label: 'Kỳ học',
            children: (
              <>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ marginBottom: 12, background: '#FF6B00' }}
                  onClick={() => {
                    form.resetFields()
                    setOpen('sem')
                  }}
                >
                  Thêm kỳ
                </Button>
                <Table
                  rowKey="id"
                  dataSource={semesters}
                  columns={[
                    { title: 'Tên', dataIndex: 'name' },
                    {
                      title: 'Bắt đầu',
                      dataIndex: 'startDate',
                      render: (d: string) => dayjs(d).format('DD/MM/YYYY'),
                    },
                    {
                      title: 'Kết thúc',
                      dataIndex: 'endDate',
                      render: (d: string) => dayjs(d).format('DD/MM/YYYY'),
                    },
                  ]}
                />
              </>
            ),
          },
          {
            key: 'sub',
            label: 'Môn học',
            children: (
              <>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ marginBottom: 12, background: '#FF6B00' }}
                  onClick={() => {
                    form.resetFields()
                    setOpen('sub')
                  }}
                >
                  Thêm môn
                </Button>
                <Table
                  rowKey="id"
                  dataSource={subjects}
                  columns={[
                    { title: 'Mã', dataIndex: 'code' },
                    { title: 'Tên', dataIndex: 'name' },
                  ]}
                />
              </>
            ),
          },
          {
            key: 'cls',
            label: 'Lớp học',
            children: (
              <>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ marginBottom: 12, background: '#FF6B00' }}
                  onClick={() => {
                    form.resetFields()
                    setOpen('cls')
                  }}
                >
                  Thêm lớp
                </Button>
                <Table
                  rowKey="id"
                  dataSource={classes}
                  columns={[
                    { title: 'Tên lớp', dataIndex: 'name' },
                    { title: 'Kỳ', dataIndex: 'semesterName' },
                    { title: 'Sĩ số', dataIndex: 'studentCount' },
                    {
                      title: 'Chủ nhiệm',
                      dataIndex: 'homeroomTeacherName',
                      render: (v: string | null) => v || '—',
                    },
                  ]}
                />
              </>
            ),
          },
        ]}
      />

      <Modal
        title={
          open === 'sem'
            ? 'Thêm kỳ học'
            : open === 'sub'
              ? 'Thêm môn'
              : 'Thêm lớp'
        }
        open={open != null}
        onCancel={() => setOpen(null)}
        onOk={() => form.submit()}
        okText="Lưu"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={async (v) => {
            try {
              if (open === 'sem') {
                await api.post('/semesters', {
                  name: v.name,
                  startDate: v.range[0].toISOString(),
                  endDate: v.range[1].toISOString(),
                })
              } else if (open === 'sub') {
                await api.post('/subjects', {
                  name: v.name,
                  code: v.code,
                })
              } else if (open === 'cls') {
                await api.post('/classes', {
                  name: v.name,
                  semesterId: v.semesterId,
                  homeroomTeacherId: v.homeroomTeacherId ?? null,
                })
              }
              message.success('Đã lưu.')
              setOpen(null)
              void load()
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Lưu thất bại.'
              message.error(msg)
            }
          }}
        >
          {open === 'sem' && (
            <>
              <Form.Item name="name" label="Tên kỳ" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item
                name="range"
                label="Thời gian"
                rules={[{ required: true }]}
              >
                <DatePicker.RangePicker style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}
          {open === 'sub' && (
            <>
              <Form.Item name="code" label="Mã môn" rules={[{ required: true }]}>
                <Input placeholder="MATH" />
              </Form.Item>
              <Form.Item name="name" label="Tên môn" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
            </>
          )}
          {open === 'cls' && (
            <>
              <Form.Item name="name" label="Tên lớp" rules={[{ required: true }]}>
                <Input placeholder="10A1" />
              </Form.Item>
              <Form.Item
                name="semesterId"
                label="Kỳ học"
                rules={[{ required: true }]}
              >
                <Select
                  options={semesters.map((s) => ({
                    value: s.id,
                    label: s.name,
                  }))}
                />
              </Form.Item>
              <Form.Item name="homeroomTeacherId" label="GV chủ nhiệm">
                <Select
                  allowClear
                  options={teachers.map((t) => ({
                    value: t.id,
                    label: `${t.fullName} (${t.phone})`,
                  }))}
                />
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>
    </>
  )
}
