import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "ETHERFALL 운영툴", template: "%s · ETHERFALL 운영툴" },
  robots: { index: false, follow: false },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ko">
      <body>{children}</body>
    </html>
  );
}
