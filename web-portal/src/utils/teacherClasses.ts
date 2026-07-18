import dayjs from 'dayjs'
import type { SemesterItem, TeacherClass } from '../types'

/** Gộp trùng classId (API trả 1 dòng / môn). */
export function uniqueHomeroomClasses(classes: TeacherClass[]): TeacherClass[] {
  const map = new Map<number, TeacherClass>()
  for (const c of classes) {
    if (c.isHomeroom) map.set(c.classId, c)
  }
  return [...map.values()].sort((a, b) =>
    (a.semesterName ?? '').localeCompare(b.semesterName ?? '', 'vi'),
  )
}

/** Nhãn dropdown: "10A1 · Học kỳ 2 (2025-2026)" — tránh nhầm nhiều lớp cùng tên. */
export function classOptionLabel(c: TeacherClass): string {
  return c.semesterName ? `${c.className} · ${c.semesterName}` : c.className
}

export function homeroomSelectOptions(classes: TeacherClass[]) {
  return uniqueHomeroomClasses(classes).map((c) => ({
    value: c.classId,
    label: classOptionLabel(c),
  }))
}

/** Ưu tiên lớp chủ nhiệm thuộc kỳ đang diễn ra; không có thì kỳ mới nhất. */
export function pickDefaultHomeroomClassId(
  classes: TeacherClass[],
  semesters: SemesterItem[] = [],
): number | undefined {
  const unique = uniqueHomeroomClasses(classes)
  if (unique.length === 0) return undefined

  const today = dayjs().startOf('day')
  const active = semesters.find((s) => {
    const from = dayjs(s.startDate).startOf('day')
    const to = dayjs(s.endDate).startOf('day')
    return !today.isBefore(from, 'day') && !today.isAfter(to, 'day')
  })
  if (active) {
    const match = unique.find((c) => c.semesterId === active.id)
    if (match) return match.classId
  }

  return [...unique].sort((a, b) => b.semesterId - a.semesterId)[0]?.classId
}
