import { Alert, Button, Card, Form, Input, Steps, Typography, message } from 'antd'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'

type Step = 0 | 1 | 2

/** Quên MK qua email OTP — gọi API /auth/forgot|verify|reset (giống app mobile). */
export function ForgotPasswordPage() {
  const nav = useNavigate()
  const [step, setStep] = useState<Step>(0)
  const [loading, setLoading] = useState(false)
  const [email, setEmail] = useState('')
  const [masked, setMasked] = useState<string | null>(null)
  const [resetToken, setResetToken] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)

  const errMsg = (e: unknown, fallback: string) =>
    (e as { response?: { data?: { message?: string } } })?.response?.data
      ?.message ?? fallback

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
      <Card style={{ width: 440, boxShadow: '0 8px 32px rgba(255,107,0,0.12)' }}>
        <Typography.Title level={3} style={{ color: '#FF6B00', marginTop: 0 }}>
          Quên mật khẩu
        </Typography.Title>
        <Typography.Paragraph type="secondary">
          Login vẫn bằng SĐT. OTP đặt lại mật khẩu gửi về email đã gắn hồ sơ.
        </Typography.Paragraph>

        <Steps
          size="small"
          current={step}
          style={{ marginBottom: 24 }}
          items={[
            { title: 'Email' },
            { title: 'OTP' },
            { title: 'Mật khẩu mới' },
          ]}
        />

        {info && (
          <Alert type="success" showIcon style={{ marginBottom: 16 }} message={info} />
        )}

        {step === 0 && (
          <Form
            layout="vertical"
            onFinish={async (v) => {
              setLoading(true)
              setInfo(null)
              try {
                const { data } = await api.post<{
                  message?: string
                  maskedDestination?: string
                }>('/auth/forgot-password', { email: v.email.trim() })
                setEmail(v.email.trim())
                setMasked(data.maskedDestination ?? null)
                setInfo(
                  data.message ??
                    'Đã gửi OTP về email. Kiểm tra hộp thư (kể cả Spam).',
                )
                setStep(1)
              } catch (e: unknown) {
                message.error(errMsg(e, 'Gửi OTP thất bại.'))
              } finally {
                setLoading(false)
              }
            }}
          >
            <Form.Item
              name="email"
              label="Email hồ sơ"
              rules={[
                { required: true, message: 'Nhập email' },
                { type: 'email', message: 'Email không hợp lệ' },
              ]}
            >
              <Input size="large" placeholder="you@gmail.com" autoComplete="email" />
            </Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              size="large"
              loading={loading}
              style={{ background: '#FF6B00' }}
            >
              Gửi OTP
            </Button>
          </Form>
        )}

        {step === 1 && (
          <Form
            layout="vertical"
            onFinish={async (v) => {
              setLoading(true)
              setInfo(null)
              try {
                const { data } = await api.post<{ resetToken?: string }>(
                  '/auth/verify-otp',
                  { email, otpCode: v.otpCode.trim() },
                )
                if (!data.resetToken) {
                  message.error('Không nhận được resetToken.')
                  return
                }
                setResetToken(data.resetToken)
                setInfo(
                  masked
                    ? `OTP đúng (gửi tới ${masked}). Nhập mật khẩu mới.`
                    : 'OTP đúng. Nhập mật khẩu mới.',
                )
                setStep(2)
              } catch (e: unknown) {
                message.error(errMsg(e, 'Xác thực OTP thất bại.'))
              } finally {
                setLoading(false)
              }
            }}
          >
            <Typography.Paragraph type="secondary">
              Mã đã gửi tới {masked ?? email}
            </Typography.Paragraph>
            <Form.Item
              name="otpCode"
              label="Mã OTP"
              rules={[
                { required: true, message: 'Nhập OTP' },
                { min: 4, message: 'OTP không hợp lệ' },
              ]}
            >
              <Input size="large" inputMode="numeric" maxLength={8} />
            </Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              size="large"
              loading={loading}
              style={{ background: '#FF6B00', marginBottom: 8 }}
            >
              Xác nhận OTP
            </Button>
            <Button
              block
              onClick={async () => {
                setLoading(true)
                try {
                  await api.post('/auth/forgot-password', { email })
                  message.success('Đã gửi lại OTP.')
                } catch (e: unknown) {
                  message.error(errMsg(e, 'Gửi lại OTP thất bại.'))
                } finally {
                  setLoading(false)
                }
              }}
            >
              Gửi lại OTP
            </Button>
          </Form>
        )}

        {step === 2 && (
          <Form
            layout="vertical"
            onFinish={async (v) => {
              if (!resetToken) {
                message.error('Thiếu resetToken. Quay lại bước OTP.')
                return
              }
              setLoading(true)
              try {
                const { data } = await api.post<{ message?: string }>(
                  '/auth/reset-password',
                  { resetToken, newPassword: v.newPassword },
                )
                message.success(data.message ?? 'Đặt lại mật khẩu thành công.')
                nav('/login')
              } catch (e: unknown) {
                message.error(errMsg(e, 'Đặt lại mật khẩu thất bại.'))
              } finally {
                setLoading(false)
              }
            }}
          >
            <Form.Item
              name="newPassword"
              label="Mật khẩu mới"
              rules={[
                { required: true, message: 'Nhập mật khẩu mới' },
                { min: 6, message: 'Tối thiểu 6 ký tự' },
              ]}
            >
              <Input.Password size="large" />
            </Form.Item>
            <Form.Item
              name="confirm"
              label="Xác nhận mật khẩu"
              dependencies={['newPassword']}
              rules={[
                { required: true, message: 'Xác nhận mật khẩu' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (!value || getFieldValue('newPassword') === value) {
                      return Promise.resolve()
                    }
                    return Promise.reject(new Error('Mật khẩu không khớp'))
                  },
                }),
              ]}
            >
              <Input.Password size="large" />
            </Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              size="large"
              loading={loading}
              style={{ background: '#FF6B00' }}
            >
              Đặt mật khẩu mới
            </Button>
          </Form>
        )}

        <div style={{ marginTop: 16, textAlign: 'center' }}>
          <Link to="/login">← Quay lại đăng nhập</Link>
        </div>
      </Card>
    </div>
  )
}
