import {
  CheckSquareOutlined,
  FormOutlined,
  ImportOutlined,
  NotificationOutlined,
  TeamOutlined,
} from '@ant-design/icons'
import { Layout, Menu, Typography, theme } from 'antd'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const { Header, Sider, Content } = Layout

const items = [
  { key: '/teacher', icon: <TeamOutlined />, label: 'Lớp của tôi' },
  {
    key: '/teacher/attendance',
    icon: <CheckSquareOutlined />,
    label: 'Điểm danh',
  },
  {
    key: '/teacher/leave',
    icon: <FormOutlined />,
    label: 'Đơn nghỉ',
  },
  {
    key: '/teacher/grades',
    icon: <ImportOutlined />,
    label: 'Import điểm',
  },
  {
    key: '/teacher/announce',
    icon: <NotificationOutlined />,
    label: 'Gửi thông báo',
  },
]

export function TeacherLayout() {
  const { user, logout } = useAuth()
  const loc = useLocation()
  const nav = useNavigate()
  const { token: t } = theme.useToken()

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider breakpoint="lg" collapsedWidth={64} theme="light">
        <div style={{ padding: 16 }}>
          <Typography.Title level={4} style={{ color: '#FF6B00', margin: 0 }}>
            FSchool
          </Typography.Title>
          <Typography.Text type="secondary">Teacher Portal</Typography.Text>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[loc.pathname]}
          items={items.map((i) => ({
            ...i,
            label: <Link to={i.key}>{i.label}</Link>,
          }))}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            background: t.colorBgContainer,
            display: 'flex',
            justifyContent: 'flex-end',
            alignItems: 'center',
            gap: 16,
            paddingInline: 24,
          }}
        >
          <Typography.Text>
            {user?.fullName} ({user?.phone})
          </Typography.Text>
          <a
            onClick={() => {
              logout()
              nav('/login')
            }}
          >
            Đăng xuất
          </a>
        </Header>
        <Content style={{ margin: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
