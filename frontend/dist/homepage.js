// Editable homepage. React and the existing authentication callback are supplied
// by the application bundle; management screens and API contracts stay intact.
export function createHomepage(React, onLogin) {
  const h = React.createElement;
  const [menuOpen, setMenuOpen] = React.useState(false);
  const login = () => { setMenuOpen(false); onLogin(); };
  const link = (href, label) => h('a', {href, onClick: () => setMenuOpen(false)}, label);
  const button = (label, secondary = false) => h('button', {type: 'button', className: secondary ? 'gh-button gh-secondary' : 'gh-button', onClick: login}, label);
  const brand = h('a', {href: '#home', className: 'gh-brand', 'aria-label': 'GYM HUNG trang chủ'}, h('span', {className: 'gh-brand-symbol', 'aria-hidden': true}, 'H'), h('span', null, 'GYM HUNG', h('small', null, 'TẬP LUYỆN & QUẢN LÝ')));
  const steps = [
    ['01', 'Chọn gói tập', 'Xem thời hạn, số buổi và mức phí của các gói đang được cung cấp.'],
    ['02', 'Sắp xếp lịch tập', 'Xem lịch lớp và huấn luyện viên, đặt buổi tập phù hợp với lịch của bạn.'],
    ['03', 'Theo dõi hành trình', 'Kiểm tra gói đã đăng ký, lịch đặt và lịch sử ra vào trong tài khoản hội viên.']
  ];
  return h('div', {className: 'gh-home'},
    h('a', {className: 'gh-skip', href: '#main-content'}, 'Đến nội dung chính'),
    h('header', {className: 'gh-header'}, h('div', {className: 'gh-wrap gh-nav'}, brand,
      h('button', {type: 'button', className: 'gh-menu', 'aria-label': menuOpen ? 'Đóng menu' : 'Mở menu', 'aria-expanded': menuOpen, 'aria-controls': 'gh-navigation', onClick: () => setMenuOpen(!menuOpen)}, menuOpen ? 'Đóng ✕' : 'Menu ☰'),
      h('nav', {id: 'gh-navigation', className: menuOpen ? 'gh-links is-open' : 'gh-links', 'aria-label': 'Điều hướng chính'}, link('#journey', 'Gói tập & lịch tập'), link('#members', 'Dành cho hội viên'), link('#questions', 'Hỏi đáp')),
      h('div', {className: 'gh-nav-login'}, button('Đăng nhập →')))),
    h('main', {id: 'main-content'},
      h('section', {id: 'home', className: 'gh-hero'}, h('div', {className: 'gh-wrap gh-hero-grid'},
        h('div', {className: 'gh-hero-copy'}, h('p', {className: 'gh-eyebrow'}, 'GYM HUNG / MỖI BUỔI TẬP ĐỀU CÓ Ý NGHĨA'),
          h('h1', null, 'Bắt đầu hôm nay.', h('br'), h('span', null, 'Khỏe hơn mỗi ngày.')),
          h('p', {className: 'gh-lead'}, 'Chọn gói tập, đặt lịch và theo dõi quá trình tập luyện. Một nơi để bạn chủ động chăm sóc sức khỏe của mình.'),
          h('div', {className: 'gh-actions'}, button('Đăng nhập / Đăng ký →'), h('a', {href: '#journey', className: 'gh-explore'}, 'Tìm hiểu trước ↓')),
          h('div', {className: 'gh-hero-note'}, h('span', {'aria-hidden': true}, '↗'), h('p', null, 'Đã là hội viên?', h('br'), h('strong', null, 'Lịch tập và gói của bạn nằm trong tài khoản.')))),
        h('div', {className: 'gh-poster', role: 'img', 'aria-label': 'Minh họa quả tạ và thông điệp duy trì tập luyện'},
          h('div', {className: 'gh-poster-top'}, h('span', null, 'GYM HUNG'), h('span', null, 'MOVE / TRAIN / REPEAT')),
          h('div', {className: 'gh-poster-title'}, 'MẠNH HƠN', h('br'), h('span', null, 'TỪNG NGÀY.')),
          h('svg', {viewBox: '0 0 600 300', className: 'gh-dumbbell', 'aria-hidden': true},
            h('g', {transform: 'rotate(-22 300 150)'},
              h('rect', {x: 150, y: 125, width: 300, height: 50, rx: 10, fill: '#b3c5bf'}),
              ...[90, 145, 400, 455].map((x, i) => h('rect', {key: x, x, y: i === 1 || i === 2 ? 40 : 62, width: 52, height: i === 1 || i === 2 ? 220 : 176, rx: 12, fill: i === 1 || i === 2 ? '#f8f4e9' : '#e1e9dd'})),
              h('path', {d: 'M215 140h170 M215 154h170 M215 167h170', stroke: '#50635e', strokeWidth: 3}))),
          h('div', {className: 'gh-poster-bottom'}, h('span', null, 'Không cần hoàn hảo.', h('br'), 'Chỉ cần bắt đầu.'), h('span', {'aria-hidden': true}, '↗'))))),
      h('section', {id: 'journey', className: 'gh-section gh-wrap'},
        h('div', {className: 'gh-section-head'}, h('div', null, h('p', {className: 'gh-eyebrow'}, 'LỘ TRÌNH CỦA BẠN'), h('h2', null, 'Tập luyện có kế hoạch.')), h('p', null, 'Từ chọn gói đến đặt lịch, các thông tin được quản lý trong tài khoản hội viên.')),
        h('div', {className: 'gh-steps'}, steps.map(([number, title, description]) => h('article', {key: number}, h('span', {className: 'gh-step-number'}, number), h('h3', null, title), h('p', null, description)))),
        h('div', {className: 'gh-package-note'}, h('p', null, 'Giá và thời hạn gói tập được hiển thị trong hệ thống sau khi đăng nhập.'), button('Xem gói tập →', true))),
      h('section', {id: 'members', className: 'gh-members'}, h('div', {className: 'gh-wrap gh-members-grid'},
        h('div', null, h('p', {className: 'gh-eyebrow'}, 'TÀI KHOẢN HỘI VIÊN'), h('h2', null, 'Bớt thao tác.', h('br'), 'Thêm thời gian tập.'), h('p', {className: 'gh-member-lead'}, 'Đăng nhập để xem thông tin của bạn và sử dụng các chức năng theo quyền tài khoản.'), button('Mở tài khoản của tôi →')),
        h('ul', {className: 'gh-member-list'}, ['Gói tập và thời hạn sử dụng', 'Lịch lớp, huấn luyện viên và đặt lịch', 'Lịch sử check-in và thanh toán', 'Hồ sơ cá nhân và trợ lý AI'].map((text, i) => h('li', {key: text}, h('span', {'aria-hidden': true}, `0${i + 1}`), text, h('span', {'aria-hidden': true}, '↗')))))),
      h('section', {id: 'questions', className: 'gh-section gh-wrap gh-faq'}, h('div', null, h('p', {className: 'gh-eyebrow'}, 'TRƯỚC KHI BẮT ĐẦU'), h('h2', null, 'Bạn cần biết gì?')),
        h('div', null,
          h('details', null, h('summary', null, 'Tôi chưa có tài khoản thì làm thế nào?'), h('p', null, 'Chọn Đăng nhập / Đăng ký, sau đó mở mục đăng ký trong màn hình tài khoản và điền thông tin để tạo tài khoản hội viên.')),
          h('details', null, h('summary', null, 'Xem giá gói tập và lịch lớp ở đâu?'), h('p', null, 'Đăng nhập để xem danh mục gói tập và lịch lớp trong hệ thống. Kiểm tra thông tin gói trước khi đăng ký.')),
          h('details', null, h('summary', null, 'Nhân viên và huấn luyện viên đăng nhập ở đâu?'), h('p', null, 'Sử dụng cùng nút Đăng nhập. Sau khi xác thực, hệ thống hiển thị chức năng theo vai trò và quyền được cấp.')))),
      h('section', {className: 'gh-start gh-wrap'}, h('div', null, h('p', {className: 'gh-eyebrow'}, 'BƯỚC TIẾP THEO'), h('h2', null, 'Sẵn sàng cho buổi tập tiếp theo?')), button('Bắt đầu với tài khoản →'))),
    h('footer', {className: 'gh-footer'}, h('div', {className: 'gh-wrap gh-footer-inner'}, brand, h('p', null, '© 2026 GYM HUNG. Hệ thống quản lý phòng gym.'), h('a', {href: '#home'}, 'Về đầu trang ↑'))));
}

