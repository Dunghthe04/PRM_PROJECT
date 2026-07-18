import { Button, Card, Form, Input, Typography, message } from 'antd'
import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { login, user, token } = useAuth()
  const nav = useNavigate()
  const [loading, setLoading] = useState(false)

  if (token && user) {
    if (user.role === 'Admin') return <Navigate to="/admin" replace />
    if (user.role === 'Teacher') return <Navigate to="/teacher" replace />
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        background: 'linear-gradient(145deg, #fff7f0 0%, #ffffff 50%, #ffe8d6 100%)',
        padding: 24,
      }}
    >
      <Card style={{ width: 420, boxShadow: '0 8px 32px rgba(255,107,0,0.12)' }}>
        <Typography.Title level={2} style={{ color: '#FF6B00', marginTop: 0 }}>
          FSchool
        </Typography.Title>
        <Typography.Paragraph type="secondary">
          Cổng quản trị Web (Admin & Giáo viên). Học sinh / phụ huynh dùng app
          mobile.
        </Typography.Paragraph>
        <Form
          layout="vertical"
          autoComplete="off"
          onFinish={async (v) => {
            setLoading(true)
            try {
              const u = await login(v.phone, v.password)
              if (u.role === 'Admin') nav('/admin')
              else if (u.role === 'Teacher') nav('/teacher')
              else {
                message.warning(
                  'Portal web chỉ dành cho Admin và Giáo viên. Dùng app mobile.',
                )
              }
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Đăng nhập thất bại.'
              message.error(msg)
            } finally {
              setLoading(false)
            }
          }}
        >
          <Form.Item
            name="phone"
            label="Số điện thoại"
            rules={[{ required: true, message: 'Nhập SĐT' }]}
          >
            <Input size="large" autoComplete="off" inputMode="tel" />
          </Form.Item>
          <Form.Item
            name="password"
            label="Mật khẩu"
            rules={[{ required: true, message: 'Nhập mật khẩu' }]}
          >
            <Input.Password size="large" autoComplete="new-password" />
          </Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            block
            size="large"
            loading={loading}
            style={{ background: '#FF6B00' }}
          >
            Đăng nhập
          </Button>
        </Form>
      </Card>
    </div>
  )
}
