import { Card, Col, Row, Statistic, Typography } from 'antd'
import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { ReportDashboard } from '../../types'

export function AdminReportsPage() {
  const [dash, setDash] = useState<ReportDashboard | null>(null)

  useEffect(() => {
    void api
      .get<ReportDashboard>('/reports/dashboard')
      .then((r) => setDash(r.data))
  }, [])

  return (
    <>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        Dashboard báo cáo
      </Typography.Title>
      <Row gutter={[16, 16]}>
        <Col xs={12} md={6}>
          <Card>
            <Statistic title="Học sinh" value={dash?.totalStudents ?? '—'} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic title="Giáo viên" value={dash?.totalTeachers ?? '—'} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic title="Lớp" value={dash?.totalClasses ?? '—'} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic title="Phụ huynh" value={dash?.totalParents ?? '—'} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic
              title="Điểm đã publish"
              value={dash?.publishedGradeCount ?? '—'}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic
              title="Chuyên cần TB %"
              value={
                dash?.averageAttendanceRate != null
                  ? Math.round(dash.averageAttendanceRate * 10) / 10
                  : '—'
              }
              suffix={dash?.averageAttendanceRate != null ? '%' : undefined}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic
              title="HĐ chờ thu"
              value={dash?.pendingInvoiceCount ?? '—'}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card>
            <Statistic
              title="Đã thu"
              value={dash?.totalPaidAmount ?? 0}
              formatter={(v) =>
                Number(v).toLocaleString('vi-VN') + ' ₫'
              }
            />
          </Card>
        </Col>
      </Row>
    </>
  )
}
