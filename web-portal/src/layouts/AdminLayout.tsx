import {
  BarChartOutlined,
  BookOutlined,
  DollarOutlined,
  NotificationOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { Layout, Menu, Typography, theme } from 'antd'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const { Header, Sider, Content } = Layout

const items = [
  { key: '/admin', icon: <BarChartOutlined />, label: 'Báo cáo' },
  { key: '/admin/users', icon: <UserOutlined />, label: 'Người dùng' },
  { key: '/admin/catalog', icon: <BookOutlined />, label: 'Danh mục' },
  {
    key: '/admin/assignments',
    icon: <TeamOutlined />,
    label: 'Phân công GV',
  },
  { key: '/admin/fees', icon: <DollarOutlined />, label: 'Học phí' },
  {
    key: '/admin/announce',
    icon: <NotificationOutlined />,
    label: 'Thông báo',
  },
]

export function AdminLayout() {
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
          <Typography.Text type="secondary">Admin Portal</Typography.Text>
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
