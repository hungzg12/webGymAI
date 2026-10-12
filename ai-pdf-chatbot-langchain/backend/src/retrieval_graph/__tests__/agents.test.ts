import { selectAgent, latestQuestion, readableAnswer } from '../agents.js';

describe('specialist routing', () => {
  test('account questions route to business data', () => {
    expect(selectAgent('tài khoản tên gì')).toBe('database');
  });
  test('converts model JSON into readable Vietnamese', () => {
    expect(readableAnswer('{"price":"590000.0000"}')).toBe('Giá gói tập là 590.000 đồng.');
    expect(readableAnswer('{"answer":"Xin chào"}')).toBe('Xin chào');
    expect(readableAnswer('Xin chào')).toBe('Xin chào');
    expect(readableAnswer('{"remainingSessions":17}')).not.toContain('17 ngày');
  });
  test('explicit fitness choice wins over database keywords', () => {
    expect(selectAgent('[agent:fitness]\nCâu hỏi mới cần trả lời: giá gói')).toBe('fitness');
  });
  test('ignores keywords in SQL context and previous turns', () => {
    expect(selectAgent('<sql_data>giá PDF</sql_data>\nCâu hỏi mới cần trả lời: bài tập chân')).toBe('fitness');
  });
  test('routes current business and document questions separately', () => {
    expect(selectAgent('Câu hỏi mới cần trả lời: giá GYM BASIC')).toBe('database');
    expect(selectAgent('Câu hỏi mới cần trả lời: tóm tắt PDF')).toBe('pdf');
  });
  test('extracts current question instead of previous turns', () => {
    expect(latestQuestion('Lịch sử: giá gói\nCâu hỏi mới cần trả lời: xin chào')).toBe('xin chào');
  });
});

