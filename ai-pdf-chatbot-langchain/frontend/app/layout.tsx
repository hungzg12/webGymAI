import type React from "react"
import type { Metadata } from "next"
import { GeistSans } from "geist/font/sans"
import { Toaster } from "@/components/ui/toaster"

import "./globals.css"

export const metadata: Metadata = {
  title: "Webgym · Trợ lý PDF",
  description: "Trợ lý hỏi đáp tài liệu PDF chạy trên máy với Ollama",
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="vi">
      <body className={GeistSans.className}>
        {children}
        <Toaster />
      </body>
    </html>
  )
}



import './globals.css'