// 보상 아이템 표시명. 실서비스에서는 게임 기획 데이터(아이템 테이블)에서 생성하거나 메타 API 로 조회한다.
export const itemCatalog: Record<number, string> = {
  1001: "에테르 주화",
  1002: "강화석",
  2001: "경험치 부스터 (1시간)",
  2002: "한정판 망토 「별의 순례자」",
  3001: "탈것 소환권",
};

export function itemName(itemId: number): string {
  return itemCatalog[itemId] ?? `아이템 #${itemId}`;
}
