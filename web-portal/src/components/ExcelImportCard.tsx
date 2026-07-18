import { DownloadOutlined, InboxOutlined } from '@ant-design/icons'
import { Alert, Button, Card, Space, Typography, Upload, message } from 'antd'
import { useState } from 'react'
import { downloadBlob, uploadExcel } from '../api/client'
import type { ExcelImportResult } from '../types'

interface Props {
  title: string
  description: string
  templateUrl: string
  templateFilename: string
  uploadUrl: string
}

/** Card tải mẫu + upload Excel dùng chung Admin/GV. */
export function ExcelImportCard({
  title,
  description,
  templateUrl,
  templateFilename,
  uploadUrl,
}: Props) {
  const [result, setResult] = useState<ExcelImportResult | null>(null)
  const [loading, setLoading] = useState(false)

  const onDownload = async () => {
    try {
      await downloadBlob(templateUrl, templateFilename)
      message.success('Đã tải file mẫu.')
    } catch {
      message.error('Không tải được file mẫu.')
    }
  }

  return (
    <Card title={title}>
      <Typography.Paragraph type="secondary">{description}</Typography.Paragraph>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<DownloadOutlined />} onClick={onDownload}>
          Tải file mẫu Excel
        </Button>
      </Space>
      <Upload.Dragger
        accept=".xlsx"
        maxCount={1}
        showUploadList={false}
        disabled={loading}
        customRequest={async ({ file, onSuccess, onError }) => {
          setLoading(true)
          setResult(null)
          try {
            const data = (await uploadExcel(
              uploadUrl,
              file as File,
            )) as ExcelImportResult
            setResult(data)
            message.success(data.message || 'Import xong.')
            onSuccess?.(data)
          } catch (e: unknown) {
            const msg =
              (e as { response?: { data?: { message?: string } } })?.response
                ?.data?.message ?? 'Import thất bại.'
            message.error(msg)
            onError?.(e as Error)
          } finally {
            setLoading(false)
          }
        }}
      >
        <p className="ant-upload-drag-icon">
          <InboxOutlined />
        </p>
        <p className="ant-upload-text">Kéo thả hoặc chọn file .xlsx</p>
      </Upload.Dragger>
      {result && (
        <Alert
          style={{ marginTop: 16 }}
          type={result.errorCount > 0 ? 'warning' : 'success'}
          message={result.message}
          description={
            <div>
              <div>
                Thành công: {result.successCount} · Bỏ qua: {result.skipCount} ·
                Lỗi: {result.errorCount}
              </div>
              {result.errors?.length > 0 && (
                <ul style={{ margin: '8px 0 0', paddingLeft: 18 }}>
                  {result.errors.slice(0, 20).map((err) => (
                    <li key={err}>{err}</li>
                  ))}
                </ul>
              )}
            </div>
          }
        />
      )}
    </Card>
  )
}
