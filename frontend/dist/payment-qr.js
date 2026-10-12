export function paymentQrCard(jsx) {
  const h = jsx.jsx;
  return h('section', {className:'payment-bank-card', 'aria-label':'Thanh toán chuyển khoản Vietcombank', children: [
    h('div',{children:[h('h2',{children:'Thanh toán bằng mã QR'}),h('p',{children:'Quét mã bằng ứng dụng ngân hàng để chuyển khoản.'}),h('dl',{children:[h('dt',{children:'Ngân hàng'}),h('dd',{children:'Vietcombank'}),h('dt',{children:'Người nhận'}),h('dd',{children:'NGUYEN VAN HUNG'}),h('dt',{children:'Số tài khoản'}),h('dd',{children:'9385077492'})]}),h('p',{className:'payment-bank-note',children:'Nhập đúng số tiền của khoản thanh toán. Nội dung chuyển khoản: GYM + mã thanh toán + tên hội viên.'}),h('p',{className:'payment-bank-note',children:'Kiểm tra tên người nhận trước khi chuyển. Nhân viên đối chiếu tiền nhận được trước khi xác nhận thanh toán.'})]}),
    h('details',{children:[h('summary',{children:'Hiển thị mã QR để chuyển khoản'}),h('img',{src:'/payment-qr-vietcombank.png',alt:'Mã QR Vietcombank của NGUYEN VAN HUNG, tài khoản 9385077492',className:'payment-bank-qr'}),h('a',{href:'/payment-qr-vietcombank.png',download:'QR-Vietcombank.png',children:'Tải ảnh mã QR'})]})
  ]});
}
