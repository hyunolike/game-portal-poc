import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Docker 이미지 용량 최소화 (node_modules 없이 server.js 로 실행)
  output: "standalone",
  poweredByHeader: false,
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Frame-Options", value: "DENY" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "same-origin" },
          // 운영툴은 검색엔진에 노출되면 안 된다
          { key: "X-Robots-Tag", value: "noindex, nofollow" },
        ],
      },
    ];
  },
};

export default nextConfig;
