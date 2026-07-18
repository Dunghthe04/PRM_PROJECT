import { Button, Input, Modal, Select, Space, Table, Tag, message } from 'antd'
import dayjs from 'dayjs'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import type { LeaveRequest, SemesterItem, TeacherClass } from '../../types'
import {
  homeroomSelectOptions,
  pickDefaultHomeroomClassId,
} from '../../utils/teacherClasses'

export function TeacherLeavePage() {
  const { user } = useAuth()
  const [classes, setClasses] = useState<TeacherClass[]>([])
  const [classId, setClassId] = useState<number | undefined>()
  const [status, setStatus] = useState<string | undefined>('Pending')
  const [items, setItems] = useState<LeaveRequest[]>([])

  const options = useMemo(() => homeroomSelectOptions(classes), [classes])

  useEffect(() => {
    if (!user) return
    void (async () => {
      try {
        const [clsRes, semRes] = await Promise.all([
          api.get<TeacherClass[]>(`/teachers/${user.id}/classes`),
          api.get<SemesterItem[]>('/semesters'),
        ])
        setClasses(clsRes.data)
        setClassId(pickDefaultHomeroomClassId(clsRes.data, semRes.data))
      } catch {
        message.error('Không tải danh sách lớp.')
      }
    })()
  }, [user])

  const load = useCallback(async () => {
    if (!classId) {
      setItems([])
      return
    }
    const { data } = await api.get<LeaveRequest[]>('/leave-requests', {
      params: { classId, status },
    })
    setItems(data)
  }, [classId, status])

  useEffect(() => {
    void load().catch(() => message.error('Không tải đơn nghỉ.'))
  }, [load])

  return (
    <>
      <Space wrap style={{ marginBottom: 16 }}>
        <Select
          style={{ minWidth: 280 }}
          placeholder="Lớp chủ nhiệm (theo kỳ)"
          value={classId}
          options={options}
          onChange={setClassId}
        />
        <Select
          allowClear
          style={{ width: 140 }}
          placeholder="Trạng thái"
          value={status}
          options={['Pending', 'Approved', 'Rejected'].map((s) => ({
            value: s,
            label: s,
          }))}
          onChange={setStatus}
        />
      </Space>
      <Table
        rowKey="id"
        dataSource={items}
        columns={[
          { title: 'HS', dataIndex: 'studentName' },
          { title: 'Lớp', dataIndex: 'className' },
          {
            title: 'Ngày nghỉ',
            dataIndex: 'date',
            render: (d: string) => dayjs(d).format('DD/MM/YYYY'),
          },
          { title: 'Lý do', dataIndex: 'reason', ellipsis: true },
          {
            title: 'TT',
            dataIndex: 'status',
            render: (s: string) => (
              <Tag
                color={
                  s === 'Approved' ? 'green' : s === 'Rejected' ? 'red' : 'orange'
                }
              >
                {s}
              </Tag>
            ),
          },
          {
            title: '',
            render: (_, row) =>
              row.status === 'Pending' ? (
                <Space>
                  <Button
                    size="small"
                    type="primary"
                    style={{ background: '#FF6B00' }}
                    onClick={async () => {
                      await api.put(`/leave-requests/${row.id}/approve`)
                      message.success('Đã duyệt.')
                      void load()
                    }}
                  >
                    Duyệt
                  </Button>
                  <Button
                    size="small"
                    danger
                    onClick={() => {
                      let reason = ''
                      Modal.confirm({
                        title: 'Từ chối đơn',
                        content: (
                          <Input.TextArea
                            placeholder="Lý do từ chối"
                            onChange={(e) => {
                              reason = e.target.value
                            }}
                          />
                        ),
                        onOk: async () => {
                          await api.put(`/leave-requests/${row.id}/reject`, {
                            rejectionReason: reason,
                          })
                          message.success('Đã từ chối.')
                          void load()
                        },
                      })
                    }}
                  >
                    Từ chối
                  </Button>
                </Space>
              ) : null,
          },
        ]}
      />
    </>
  )
}
