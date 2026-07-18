import { PlusOutlined } from '@ant-design/icons'
import {
  Button,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Table,
  Tabs,
  Tag,
  message,
} from 'antd'
import dayjs from 'dayjs'
import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { ClassItem, FeeCategory, FeeInvoice } from '../../types'

export function AdminFeesPage() {
  const [categories, setCategories] = useState<FeeCategory[]>([])
  const [invoices, setInvoices] = useState<FeeInvoice[]>([])
  const [classes, setClasses] = useState<ClassItem[]>([])
  const [openCat, setOpenCat] = useState(false)
  const [openBatch, setOpenBatch] = useState(false)
  const [catForm] = Form.useForm()
  const [batchForm] = Form.useForm()

  const load = useCallback(async () => {
    const [c, inv, cls] = await Promise.all([
      api.get<FeeCategory[]>('/fee-categories'),
      api.get<FeeInvoice[]>('/fee-invoices'),
      api.get<ClassItem[]>('/classes'),
    ])
    setCategories(c.data)
    setInvoices(inv.data)
    setClasses(cls.data)
  }, [])

  useEffect(() => {
    void load().catch(() => message.error('Không tải học phí.'))
  }, [load])

  return (
    <>
      <Tabs
        items={[
          {
            key: 'cat',
            label: 'Loại khoản thu',
            children: (
              <>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ marginBottom: 12, background: '#FF6B00' }}
                  onClick={() => {
                    catForm.resetFields()
                    setOpenCat(true)
                  }}
                >
                  Thêm loại phí
                </Button>
                <Table
                  rowKey="id"
                  dataSource={categories}
                  columns={[
                    { title: 'Tên', dataIndex: 'name' },
                    {
                      title: 'Mặc định',
                      dataIndex: 'defaultAmount',
                      render: (n: number) =>
                        n.toLocaleString('vi-VN') + ' ₫',
                    },
                    {
                      title: 'Active',
                      dataIndex: 'isActive',
                      render: (v: boolean) =>
                        v ? <Tag color="green">Yes</Tag> : <Tag>No</Tag>,
                    },
                  ]}
                />
              </>
            ),
          },
          {
            key: 'inv',
            label: 'Hóa đơn',
            children: (
              <>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ marginBottom: 12, background: '#FF6B00' }}
                  onClick={() => {
                    batchForm.resetFields()
                    setOpenBatch(true)
                  }}
                >
                  Gán phí theo lớp
                </Button>
                <Table
                  rowKey="id"
                  dataSource={invoices}
                  columns={[
                    { title: 'HS', dataIndex: 'studentName' },
                    { title: 'SĐT', dataIndex: 'studentPhone' },
                    { title: 'Khoản', dataIndex: 'feeCategoryName' },
                    {
                      title: 'Số tiền',
                      dataIndex: 'amount',
                      render: (n: number) =>
                        n.toLocaleString('vi-VN') + ' ₫',
                    },
                    {
                      title: 'Hạn',
                      dataIndex: 'dueDate',
                      render: (d: string) => dayjs(d).format('DD/MM/YYYY'),
                    },
                    {
                      title: 'TT',
                      dataIndex: 'status',
                      render: (s: string) => (
                        <Tag color={s === 'Paid' ? 'green' : 'orange'}>{s}</Tag>
                      ),
                    },
                  ]}
                />
              </>
            ),
          },
        ]}
      />

      <Modal
        title="Loại khoản thu"
        open={openCat}
        onCancel={() => setOpenCat(false)}
        onOk={() => catForm.submit()}
      >
        <Form
          form={catForm}
          layout="vertical"
          initialValues={{ isActive: true, defaultAmount: 1000000 }}
          onFinish={async (v) => {
            try {
              await api.post('/fee-categories', v)
              message.success('Đã tạo.')
              setOpenCat(false)
              void load()
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Lỗi.'
              message.error(msg)
            }
          }}
        >
          <Form.Item name="name" label="Tên" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Mô tả">
            <Input.TextArea />
          </Form.Item>
          <Form.Item
            name="defaultAmount"
            label="Số tiền mặc định"
            rules={[{ required: true }]}
          >
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Gán phí cả lớp"
        open={openBatch}
        onCancel={() => setOpenBatch(false)}
        onOk={() => batchForm.submit()}
      >
        <Form
          form={batchForm}
          layout="vertical"
          onFinish={async (v) => {
            try {
              await api.post('/fee-invoices/batch', {
                classId: v.classId,
                feeCategoryId: v.feeCategoryId,
                amount: v.amount,
                dueDate: v.dueDate.toISOString(),
                note: v.note,
              })
              message.success('Đã tạo hóa đơn lớp.')
              setOpenBatch(false)
              void load()
            } catch (e: unknown) {
              const msg =
                (e as { response?: { data?: { message?: string } } })?.response
                  ?.data?.message ?? 'Lỗi.'
              message.error(msg)
            }
          }}
        >
          <Form.Item name="classId" label="Lớp" rules={[{ required: true }]}>
            <Select
              options={classes.map((c) => ({ value: c.id, label: c.name }))}
            />
          </Form.Item>
          <Form.Item
            name="feeCategoryId"
            label="Loại phí"
            rules={[{ required: true }]}
          >
            <Select
              options={categories.map((c) => ({
                value: c.id,
                label: c.name,
              }))}
            />
          </Form.Item>
          <Form.Item name="amount" label="Số tiền (để trống = mặc định)">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
          <Form.Item name="dueDate" label="Hạn nộp" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
