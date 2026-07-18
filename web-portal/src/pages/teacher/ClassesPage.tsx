import { Tag, Table, Typography, message } from 'antd'
import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import type { TeacherClass } from '../../types'

export function TeacherClassesPage() {
  const { user } = useAuth()
  const [items, setItems] = useState<TeacherClass[]>([])

  useEffect(() => {
    if (!user) return
    void api
      .get<TeacherClass[]>(`/teachers/${user.id}/classes`)
      .then((r) => setItems(r.data))
      .catch(() => message.error('Không tải lớp.'))
  }, [user])

  return (
    <>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        Lớp & môn phụ trách
      </Typography.Title>
      <Table
        rowKey={(r) => `${r.assignmentId}-${r.classId}-${r.subjectId}`}
        dataSource={items}
        columns={[
          { title: 'Lớp', dataIndex: 'className' },
          { title: 'Môn', dataIndex: 'subjectName' },
          { title: 'Mã', dataIndex: 'subjectCode' },
          { title: 'Kỳ', dataIndex: 'semesterName' },
          {
            title: 'Vai trò',
            dataIndex: 'isHomeroom',
            render: (v: boolean) =>
              v ? (
                <Tag color="orange">Chủ nhiệm</Tag>
              ) : (
                <Tag>Bộ môn</Tag>
              ),
          },
        ]}
      />
    </>
  )
}
