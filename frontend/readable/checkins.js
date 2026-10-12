import {d as usageApi} from "./index-unified-ollama-v2.js";
import {PackageUsagePanel, eligiblePackages, packageLabel} from "./package-usage.js";
import {
  H as e,
  J as t,
  K as n,
  R as r,
  Z as i,
  _ as a,
  ct as o,
  dn as s,
  et as c,
  g as l,
  ht as u,
  ln as d,
  lt as f,
  m as p,
  mn as m,
  q as h,
  rt as g,
  t as _,
  tt as v,
  ut as y,
  v as b,
  y as x,
} from "./index-unified-ollama-v2.js";
import { t as S } from "./module-tabs-BKE84y-f-v2.js";
var C = m(s(), 1),
  w = d();
function T() {
  let { data: s, session: d, mutate: m } = c(),
    [T, E] = (0, C.useState)(``),
    [D, O] = (0, C.useState)(null),
    [selectedPackage, setSelectedPackage] = (0,C.useState)({});
  if (!s) return null;
  let open = s.CheckIns.filter((e) => e.status === `CheckedIn`),
    k = open.filter((e) => e.checkInTime?.startsWith(x())),
    stale = open.filter((e) => !e.checkInTime?.startsWith(x())),
    A = T
      ? s.Members.filter((e) => {
          let t = s.Users.find((t) => t.userId === e.userId);
          return l(`${t?.fullName} ${t?.email} ${t?.phone}`).includes(l(T));
        }).slice(0, 6)
      : [];
  async function j(e, t) {
    O(e);
    try {
      await m((service) => t
        ? service.save('CheckIns',t,{status:'CheckedOut'})
        : usageApi('/api/package-usage/check-in',{method:'POST',body:JSON.stringify({memberId:e,memberPackageId:selectedPackage[e] || null})}),
        t ? 'Đã check-out' : 'Check-in và sử dụng gói thành công');
    } catch {
    } finally {
      O(null);
    }
  }
  let M = (0, w.jsxs)(`div`, {
    className: `checkin-grid`,
    children: [
      (0, w.jsxs)(`section`, {
        className: `panel`,
        children: [
          (0, w.jsx)(h, {
            title: `Check-in nhanh`,
            description: `Tìm theo tên, số điện thoại hoặc email.`,
          }),
          (0, w.jsxs)(`div`, {
            className: `checkin-search`,
            children: [
              (0, w.jsx)(f, { size: 18 }),
              (0, w.jsx)(r, {
                "aria-label": `Tìm hội viên check-in`,
                placeholder: `Nhập tên, email hoặc số điện thoại…`,
                value: T,
                onChange: (e) => E(e.target.value),
              }),
            ],
          }),
          (0, w.jsx)(`div`, {
            className: `checkin-results`,
            children: T
              ? A.length
                ? A.map((e) => {
                    let t = s.Users.find((t) => t.userId === e.userId),
                      n = open.find((t) => t.memberId === e.memberId),
                      r = eligiblePackages(s,e.memberId).length > 0;
                    return (0, w.jsxs)(
                      `div`,
                      {
                        className: `checkin-result`,
                        children: [
                          (0, w.jsxs)(`div`, {
                            className: `person-row`,
                            children: [
                              (0, w.jsx)(`span`, {
                                className: `avatar`,
                                children: b(t?.fullName ?? ``),
                              }),
                              (0, w.jsxs)(`span`, {
                                className: `person-name`,
                                children: [
                                  (0, w.jsx)(`strong`, {
                                    children: t?.fullName,
                                  }),
                                  (0, w.jsx)(`small`, { children: t?.phone }),
                                ],
                              }),
                              i(d, `checkins.write`) &&
                                (0, w.jsx)(v, {
                                  size: `sm`,
                                  disabled:
                                    D === e.memberId ||
                                    (!n && (!r || !selectedPackage[e.memberId] || e.status !== `Active`)),
                                  onClick: () =>
                                    void j(e.memberId, n?.checkInId),
                                  children: n
                                    ? n.checkInTime?.startsWith(x())
                                      ? `Check-out`
                                      : `Xử lý lượt cũ`
                                    : `Check-in`,
                                }),
                            ],
                          }),
                          !n && i(d,'checkins.write') && (0,w.jsx)('select', {
                            'aria-label':`Gói sử dụng của ${t?.fullName}`,
                            value:selectedPackage[e.memberId] || '',
                            onChange:ev=>setSelectedPackage({...selectedPackage,[e.memberId]:ev.target.value}),
                            children:[(0,w.jsx)('option',{value:'',children:'Chọn gói đã thanh toán'}),
                              ...eligiblePackages(s,e.memberId).map(p=>(0,w.jsx)('option',{value:p.memberPackageId,children:packageLabel(s,p)},p.memberPackageId))]
                          }),
                          !r &&
                            (0, w.jsxs)(`p`, {
                              className: `field-error`,
                              children: [
                                (0, w.jsx)(o, { size: 14 }),
                                `Chưa có gói vào phòng đã thanh toán và còn hiệu lực.`,
                              ],
                            }),
                          e.status !== `Active` &&
                            (0, w.jsx)(`p`, {
                              className: `field-error`,
                              children: `Hội viên đang ngừng hoạt động hoặc tạm khóa.`,
                            }),
                        ],
                      },
                      e.memberId,
                    );
                  })
                : (0, w.jsx)(e, { title: `Không tìm thấy hội viên` })
              : (0, w.jsxs)(`div`, {
                  className: `checkin-ready`,
                  children: [
                    (0, w.jsx)(y, { size: 56 }),
                    (0, w.jsx)(`h3`, { children: `Sẵn sàng đón hội viên` }),
                    (0, w.jsx)(`p`, {
                      children: `Nhập thông tin hội viên để ghi nhận lượt vào phòng tập.`,
                    }),
                  ],
                }),
          }),
        ],
      }),
      (0, w.jsxs)(`section`, {
        className: `panel`,
        children: [
          (0, w.jsx)(h, {
            title: `Đang có mặt tại phòng tập`,
            description: `${k.length} hội viên hiện tại`,
          }),
          k.length
            ? k.map((e) =>
                (0, w.jsxs)(
                  `div`,
                  {
                    className: `person-row`,
                    children: [
                      (0, w.jsx)(`span`, {
                        className: `avatar`,
                        children: b(t(`memberId`, e.memberId, s)),
                      }),
                      (0, w.jsxs)(`span`, {
                        className: `person-name`,
                        children: [
                          (0, w.jsx)(`strong`, {
                            children: t(`memberId`, e.memberId, s),
                          }),
                          (0, w.jsxs)(`small`, {
                            children: [`Vào lúc `, p(e.checkInTime, !0)],
                          }),
                        ],
                      }),
                      i(d, `checkins.write`) &&
                        (0, w.jsx)(v, {
                          variant: `outline`,
                          size: `sm`,
                          "aria-label": `Check-out ${t(`memberId`, e.memberId, s)}`,
                          disabled: D === e.memberId,
                          onClick: () => void j(e.memberId, e.checkInId),
                          children: (0, w.jsx)(u, { size: 15 }),
                        }),
                    ],
                  },
                  e.checkInId,
                ),
              )
            : (0, w.jsx)(e, { title: `Chưa có hội viên tại phòng tập` }),
        ],
      }),
      stale.length
        ? (0, w.jsxs)(`section`, {
            className: `panel checkin-stale`,
            children: [
              (0, w.jsx)(h, {
                title: `Lượt cũ cần kiểm tra`,
                description: `${stale.length} lượt từ ngày trước chưa check-out. Kiểm tra lịch sử trước khi xử lý.`,
              }),
              stale.length
                ? stale.map((e) =>
                    (0, w.jsxs)(
                      `div`,
                      {
                        className: `person-row`,
                        children: [
                          (0, w.jsx)(`span`, {
                            className: `avatar`,
                            children: b(t(`memberId`, e.memberId, s)),
                          }),
                          (0, w.jsxs)(`span`, {
                            className: `person-name`,
                            children: [
                              (0, w.jsx)(`strong`, {
                                children: t(`memberId`, e.memberId, s),
                              }),
                              (0, w.jsxs)(`small`, {
                                children: [`Vào lúc `, p(e.checkInTime, !0)],
                              }),
                            ],
                          }),
                          i(d, `checkins.write`) &&
                            (0, w.jsx)(v, {
                              variant: `outline`,
                              size: `sm`,
                              "aria-label": `Check-out ${t(`memberId`, e.memberId, s)}`,
                              disabled: D === e.memberId,
                              onClick: () => void j(e.memberId, e.checkInId),
                              children: (0, w.jsx)(u, { size: 15 }),
                            }),
                        ],
                      },
                      e.checkInId,
                    ),
                  )
                : (0, w.jsx)(e, { title: `Không có lượt cũ` }),
            ],
          })
        : null,
    ],
  });
  return (0, w.jsxs)(w.Fragment, {
    children: [
      (0, w.jsx)(n, {
        eyebrow: `VẬN HÀNH TRỰC TIẾP`,
        title: `Check-in & điểm danh`,
        description: `Ghi nhận mỗi lượt đến. Theo dõi phòng tập trong ngày.`,
      }),
      (0, w.jsxs)(`div`, {
        className: `summary-row`,
        children: [
          (0, w.jsxs)(`div`, {
            children: [
              (0, w.jsx)(g, {}),
              (0, w.jsxs)(`span`, {
                children: [
                  `Đang có mặt`,
                  (0, w.jsx)(`strong`, { children: k.length }),
                ],
              }),
            ],
          }),
          (0, w.jsxs)(`div`, {
            children: [
              (0, w.jsx)(y, {}),
              (0, w.jsxs)(`span`, {
                children: [
                  `Check-in hôm nay`,
                  (0, w.jsx)(`strong`, {
                    children: s.CheckIns.filter((e) =>
                      e.checkInTime.startsWith(x()),
                    ).length,
                  }),
                ],
              }),
            ],
          }),
        ],
      }),
      (0, w.jsx)(PackageUsagePanel, {}),
      (0, w.jsx)(S, {
        tabs: [
          { id: `live`, title: `Quầy check-in`, content: M },
          {
            id: `history`,
            title: `Lịch sử ra vào`,
            content: (0, w.jsx)(_, { table: `CheckIns` }),
          },
          {
            id: `attendance`,
            title: `Điểm danh theo booking`,
            content: (0, w.jsx)(_, { table: `Attendance` }),
          },
        ],
      }),
    ],
  });
}
export { T as CheckInsPage };
