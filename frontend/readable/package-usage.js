import {dn as reactFactory, mn as compat, et as useWorkspace, d as apiRequest, Z as can} from './index-unified-ollama-v2.js';
const React = compat(reactFactory(), 1), h = React.createElement;
export const isPt = name => /PERSONAL TRAINING|huấn luyện/i.test(name ?? '') || name === 'PT';
export function eligiblePackages(data, memberId, trainer = false) {
  const today = new Intl.DateTimeFormat('en-CA', {timeZone: 'Asia/Ho_Chi_Minh'}).format(new Date());
  return data.MemberPackages.filter(p => p.memberId === memberId && p.status === 'Active' && p.startDate <= today
    && (!p.endDate || p.endDate >= today) && (p.remainingSessions == null || p.remainingSessions > 0)
    && isPt(data.Packages.find(c => c.packageId === p.packageId)?.packageName) === trainer
    && data.Payments.some(pay => pay.memberPackageId === p.memberPackageId && pay.status === 'Completed' && Number(pay.amount) === Number(p.price)));
}
export function packageLabel(data, p) {
  return `${data.Packages.find(c => c.packageId === p.packageId)?.packageName ?? 'Gói tập'} · #${p.memberPackageId} · ${p.remainingSessions == null ? 'không giới hạn buổi' : `${p.remainingSessions} buổi còn lại`} · ${p.endDate ? `hạn ${p.endDate}` : 'không giới hạn ngày'}`;
}
export function PackageUsagePanel() {
  const {data, session, mutate} = useWorkspace();
  const [bookingId, setBooking] = React.useState(''), [registrationId, setRegistration] = React.useState(''),
    [busy, setBusy] = React.useState(false), [error, setError] = React.useState('');
  if (!data) return null;
  const today = new Intl.DateTimeFormat('en-CA', {timeZone: 'Asia/Ho_Chi_Minh'}).format(new Date());
  const bookings = data.Bookings.filter(b => b.status === 'Confirmed' && !data.Attendance.some(a => a.bookingId === b.bookingId)
    && data.WorkoutSchedules.some(s => s.workoutScheduleId === b.workoutScheduleId && s.scheduleDate === today && s.status !== 'Canceled'));
  const booking = bookings.find(b => b.bookingId === bookingId);
  const registrations = booking ? eligiblePackages(data, booking.memberId, true) : [];
  const name = memberId => {
    const member = data.Members.find(m => m.memberId === memberId);
    return data.Users.find(u => u.userId === member?.userId)?.fullName ?? `Hội viên #${memberId}`;
  };
  async function attend(e) {
    e.preventDefault(); setBusy(true); setError('');
    try {
      await mutate(() => apiRequest('/api/package-usage/attendance', {method: 'POST', body: JSON.stringify({bookingId, memberPackageId: registrationId})}), 'Đã điểm danh và trừ một buổi PT');
      setBooking(''); setRegistration('');
    } catch (err) { setError(err.message ?? 'Không thể điểm danh buổi PT.'); }
    finally { setBusy(false); }
  }
  return h('section', {className: 'panel', style: {padding: '20px', marginBottom: '20px'}},
    h('h2', null, 'Đóng tiền và sử dụng gói tập'),
    h('p', null, 'Thanh toán một lần khi đăng ký. Mỗi lượt dùng gói không thu tiền thêm. Gia hạn tạo đăng ký mới để giữ lịch sử.'),
    h('ul', {style: {paddingLeft: '20px', lineHeight: '1.8'}},
      h('li', null, 'Vé ngày: DAY PASS có 1 buổi trong 1 ngày; check-in trừ 1 buổi.'),
      h('li', null, 'Gói theo thời hạn: kiểm tra ngày hết hạn và hạn mức buổi đã mua. Gói không giới hạn buổi chỉ ghi lượt, không giảm số buổi.'),
      h('li', null, 'Gói PT: trừ 1 buổi khi điểm danh lịch với PT được phân công; lượt vào phòng không trừ gói PT. Cần gói vào phòng riêng.'),
      h('li', null, 'Chỉ sử dụng gói đã thanh toán đầy đủ. Gửi lại yêu cầu hoặc check-out không trừ thêm buổi.')),
    can(session, 'checkins.write') && h('form', {onSubmit: attend, style: {display: 'grid', gap: '12px', marginTop: '16px'}},
      h('h3', null, 'Điểm danh buổi PT hôm nay'),
      h('label', null, 'Lịch đã đặt', h('select', {value: bookingId, required: true, onChange: e => {setBooking(e.target.value); setRegistration('');}, style: {display: 'block', width: '100%', padding: '10px', color: 'inherit', background: 'transparent'}},
        h('option', {value: ''}, 'Chọn lịch đã xác nhận'), ...bookings.map(b => {
          const s = data.WorkoutSchedules.find(s => s.workoutScheduleId === b.workoutScheduleId);
          return h('option', {key: b.bookingId, value: b.bookingId}, `${name(b.memberId)} · ${s.title ?? 'Buổi PT'} · ${s.startTime}`);
        }))),
      h('label', null, 'Gói PT dùng cho buổi này', h('select', {value: registrationId, required: true, onChange: e => setRegistration(e.target.value), style: {display: 'block', width: '100%', padding: '10px', color: 'inherit', background: 'transparent'}},
        h('option', {value: ''}, 'Chọn gói PT đã thanh toán'), ...registrations.map(p => h('option', {key: p.memberPackageId, value: p.memberPackageId}, packageLabel(data, p))))),
      booking && !registrations.length && h('p', {className: 'field-error'}, 'Hội viên chưa có gói PT đã thanh toán còn hiệu lực.'),
      error && h('p', {role: 'alert', className: 'field-error'}, error),
      h('button', {type: 'submit', disabled: busy || !bookingId || !registrationId, style: {padding: '10px 16px', borderRadius: '8px', background: '#009879', color: 'white', width: 'fit-content'}}, busy ? 'Đang ghi nhận…' : 'Xác nhận sử dụng 1 buổi PT')));
}
