export type UserRole = 'Admin' | 'Teacher' | 'Parent' | 'Student'

export interface User {
  id: number
  phone: string
  fullName: string
  avatarUrl: string
  email?: string | null
  isPhoneVerified: boolean
  isLocked: boolean
  role: UserRole | string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ExcelImportResult {
  successCount: number
  skipCount: number
  errorCount: number
  message: string
  errors: string[]
}

export interface ClassItem {
  id: number
  name: string
  semesterId: number
  semesterName?: string | null
  studentCount: number
  homeroomTeacherId?: number | null
  homeroomTeacherName?: string | null
}

export interface SubjectItem {
  id: number
  name: string
  code: string
}

export interface SemesterItem {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface ClassStudent {
  studentId: number
  phone: string
  fullName: string
}

export interface TeacherClass {
  assignmentId: number
  classId: number
  className: string
  subjectId: number
  subjectName: string
  subjectCode: string
  semesterId: number
  semesterName?: string | null
  isHomeroom: boolean
}

export interface AttendanceRecord {
  id: number
  classId: number
  className: string
  studentId: number
  studentName: string
  studentPhone: string
  date: string
  status: string
}

export interface LeaveRequest {
  id: number
  classId: number
  className: string
  studentId: number
  studentName: string
  studentPhone: string
  date: string
  reason: string
  medicalCertificateUrl?: string | null
  status: string
  rejectionReason?: string | null
  createdAt: string
}

export interface Announcement {
  id: number
  title: string
  content: string
  type: string
  targetClassId?: number | null
  targetClassName?: string | null
  subjectId?: number | null
  subjectName?: string | null
  targetUserId?: number | null
  targetUserName?: string | null
  createdByName: string
  createdAt: string
}

export interface FeeCategory {
  id: number
  name: string
  description?: string | null
  defaultAmount: number
  isActive: boolean
}

export interface FeeInvoice {
  id: number
  studentId: number
  studentName: string
  studentPhone: string
  feeCategoryId: number
  feeCategoryName: string
  amount: number
  dueDate: string
  status: string
  isPaid: boolean
  paidAt?: string | null
}

export interface TeacherAssignment {
  id: number
  teacherId: number
  teacherName: string
  teacherPhone: string
  classId: number
  className: string
  subjectId: number
  subjectName: string
  subjectCode: string
}

export interface ReportDashboard {
  totalStudents: number
  totalTeachers: number
  totalClasses: number
  totalParents: number
  publishedGradeCount: number
  averageAttendanceRate?: number | null
  pendingInvoiceCount: number
  paidInvoiceCount: number
  totalPaidAmount: number
  totalPendingAmount: number
  generatedAt: string
}
