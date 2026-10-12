import type { Metadata } from 'next';
import { Be_Vietnam_Pro } from 'next/font/google';
import './globals.css';
const beVietnamPro = Be_Vietnam_Pro({
  subsets: ['vietnamese'],
  weight: ['400', '500', '600', '700', '800'],
  variable: '--font-be-vietnam-pro',
});
export const metadata: Metadata = {
  title: 'GYM HUNG — Quản lý phòng gym',
  description:
    'GYM HUNG: quản lý hội viên, lịch tập, thanh toán, thiết bị và vận hành phòng gym.',
};
export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi" className="dark">
      <body className={beVietnamPro.variable}>{children}</body>
    </html>
  );
}
