import {
  LockOutlined,
  PlusOutlined,
  UnlockOutlined,
  KeyOutlined,
} from '@ant-design/icons'
import {
  Button,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  message,
} from 'antd'
import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/client'
import { ExcelImportCard } from '../../components/ExcelImportCard'
import { GradesImportWizard } from '../../components/GradesImportWizard'
import type { PagedResult, User } from '../../types'

export function AdminUsersPage() {
  const [data, setData] = useState<PagedResult<User> | null>(null)
  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [role, setRole] = useState<string | undefined>()
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data: res } = await api.get<PagedResult<User>>('/users', {
        params: { page, pageSize: 15, role, search: search || undefined },
      })
      setData(res)
    } catch {
      message.error('Không tải được danh sách user.')
    } finally {
      setLoading(false)
    }
  }, [page, role, search])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <Tabs
      items={[
        {
          key: 'list',
          label: 'Danh sách',
          children: (
            <>
              <Space wrap style={{ marginBottom: 16 }}>
                <Input.Search
                  placeholder="SĐT / họ tên"
                  allowClear
                  onSearch={(v) => {
                    setPage(1)
                    setSearch(v)
                  }}
                  style={{ width: 220 }}
                />
                <Select
                  allowClear
                  placeholder="Vai trò"
                  style={{ width: 140 }}
                  options={['Admin', 'Teacher', 'Parent', 'Student'].map(
                    (r) => ({ value: r, label: r }),
                  )}
                  onChange={(v) => {
                    setPage(1)
                    setRole(v)
                  }}
                />
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ background: '#FF6B00' }}
                  onClick={() => setCreateOpen(true)}
                >
                  Thêm user
                </Button>
              </Space>
              <Table
                rowKey="id"
                loading={loading}
                dataSource={data?.items}
                pagination={{
                  current: page,
                  pageSize: 15,
                  total: data?.totalCount,
                  onChange: setPage,
                }}
                columns={[
                  { title: 'Họ tên', dataIndex: 'fullName' },
                  { title: 'SĐT', dataIndex: 'phone' },
                  {
                    title: 'Role',
                    dataIndex: 'role',
                    render: (r: string) => <Tag color="orange">{r}</Tag>,
                  },
                  {
                    title: 'Trạng thái',
                    dataIndex: 'isLocked',
                    render: (locked: boolean) =>
                      locked ? (
                        <Tag color="red">Khóa</Tag>
                      ) : (
                        <Tag color="green">OK</Tag>
                      ),
                  },
                  {
                    title: 'Thao tác',
                    render: (_, row) => (
                      <Space>
                        {row.isLocked ? (
                          <Button
                            size="small"
                            icon={<UnlockOutlined />}
                            onClick={async () => {
                              await api.put(`/users/${row.id}/unlock`)
                              message.success('Đã mở khóa.')
                              void load()
                            }}
                          >
                            Mở
                          </Button>
                        ) : (
                          <Button
                            size="small"
                            danger
                            icon={<LockOutlined />}
                            onClick={async () => {
                              await api.put(`/users/${row.id}/lock`)
                              message.success('Đã khóa.')
                              void load()
                            }}
                          >
                            Khóa
                          </Button>
                        )}
                        <Button
                          size="small"
                          icon={<KeyOutlined />}
                          onClick={() => {
                            Modal.confirm({
                              title: `Reset mật khẩu — ${row.fullName}?`,
                              content: 'Mật khẩu mới mặc định: 123456',
                              onOk: async () => {
                                await api.post(
                                  `/users/${row.id}/reset-password`,
                                  { newPassword: '123456' },
                                )
                                message.success('Đã reset MK = 123456')
                              },
                            })
                          }}
                        >
                          Reset MK
                        </Button>
                      </Space>
                    ),
                  },
                ]}
              />
              <Modal
                title="Thêm người dùng"
                open={createOpen}
                onCancel={() => setCreateOpen(false)}
                onOk={() => form.submit()}
                okText="Tạo"
              >
                <Form
                  form={form}
                  layout="vertical"
                  onFinish={async (v) => {
                    try {
                      await api.post('/users', {
                        ...v,
                        isPhoneVerified: true,
                      })
                      message.success('Đã tạo.')
                      setCreateOpen(false)
                      form.resetFields()
                      void load()
                    } catch (e: unknown) {
                      const msg =
                        (e as { response?: { data?: { message?: string } } })
                          ?.response?.data?.message ?? 'Tạo thất bại.'
                      message.error(msg)
                    }
                  }}
                >
                  <Form.Item name="phone" label="SĐT" rules={[{ required: true }]}>
                    <Input />
                  </Form.Item>
                  <Form.Item
                    name="password"
                    label="Mật khẩu"
                    initialValue="123456"
                    rules={[{ required: true }]}
                  >
                    <Input.Password />
                  </Form.Item>
                  <Form.Item
                    name="fullName"
                    label="Họ tên"
                    rules={[{ required: true }]}
                  >
                    <Input />
                  </Form.Item>
                  <Form.Item name="email" label="Email">
                    <Input />
                  </Form.Item>
                  <Form.Item
                    name="role"
                    label="Vai trò"
                    initialValue="Student"
                    rules={[{ required: true }]}
                  >
                    <Select
                      options={['Admin', 'Teacher', 'Parent', 'Student'].map(
                        (r) => ({ value: r, label: r }),
                      )}
                    />
                  </Form.Item>
                </Form>
              </Modal>
            </>
          ),
        },
        {
          key: 'import',
          label: 'Import user Excel',
          children: (
            <ExcelImportCard
              title="Import tài khoản (HS / GV / PH / Admin)"
              description="Cột: Phone, Password, FullName, Email, Role, ClassName (ClassName chỉ dùng cho Student — tự ghi danh vào lớp nếu tồn tại)."
              templateUrl="/import/users/template"
              templateFilename="FSchool_User_Import_Template.xlsx"
              uploadUrl="/import/users"
            />
          ),
        },
        {
          key: 'import-grades',
          label: 'Import điểm Excel',
          children: <GradesImportWizard mode="admin" />,
        },
      ]}
    />
  )
}
