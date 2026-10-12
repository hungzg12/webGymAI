(() => {
  let authorization = '';
  let agentMode = 'auto';
  const originalFetch = window.fetch.bind(window);
  window.fetch = (input, init) => {
    const url = typeof input === 'string' ? input : input instanceof Request ? input.url : String(input);
    if (url.startsWith('http://localhost:5042/') || url.startsWith('http://127.0.0.1:5042/')) {
      const headers = new Headers(init?.headers || (input instanceof Request ? input.headers : undefined));
      const token = headers.get('Authorization');
      if (token) authorization = token;
      if (/\/api\/ai\/conversations\/\d+\/messages$/.test(url) && typeof init?.body === 'string') {
        const payload = JSON.parse(init.body);
        if (agentMode !== 'auto' && typeof payload.message === 'string') {
          payload.message = `[agent:${agentMode}]${payload.message}`;
          init = { ...init, body: JSON.stringify(payload) };
        }
      }
    }
    return originalFetch(input, init);
  };
  function attach() {
    document.getElementById('gym-pdf-chatbot')?.remove();
    if (location.pathname !== '/ai' || document.getElementById('gym-ai-upload')) return;
    const heading = [...document.querySelectorAll('h1')].find(h => h.textContent.includes('Trợ lý AI'));
    if (!heading) return;
    const row = document.createElement('div');
    row.id = 'gym-ai-upload';
    row.style.cssText = 'display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin:12px 0';
    const agentLabel = document.createElement('label');
    agentLabel.textContent = 'Tác nhân: ';
    const selector = document.createElement('select');
    selector.setAttribute('aria-label', 'Chọn tác nhân AI');
    selector.style.cssText = 'padding:10px;border:1px solid #cbd5e1;border-radius:6px;background:white;color:#1e293b;font:inherit';
    for (const [value, title] of [['auto','Tự động chọn'],['fitness','Tư vấn tập luyện'],['database','Dữ liệu phòng gym'],['pdf','Đọc PDF']]) {
      const option = document.createElement('option'); option.value = value; option.textContent = title; selector.append(option);
    }
    selector.value = agentMode;
    selector.onchange = () => { agentMode = selector.value; };
    agentLabel.append(selector);
    const button = document.createElement('button');
    button.textContent = 'Tải PDF để hỏi';
    button.type = 'button';
    button.style.cssText = 'background:#059669;color:white;border:0;border-radius:8px;padding:10px 16px;cursor:pointer;font:inherit';
    const input = document.createElement('input');
    input.type = 'file'; input.accept = '.pdf'; input.multiple = true; input.hidden = true;
    const status = document.createElement('span'); status.setAttribute('role','status');
    status.textContent = 'Hỏi dữ liệu SQL Server hoặc nội dung PDF bằng Ollama.';
    status.style.cssText = 'font-size:14px;color:#475569';
    button.onclick = () => input.click();
    input.onchange = async () => {
      if (!input.files.length) return;
      if (!authorization) { status.textContent = 'Tải lại trang và đăng nhập để tải tài liệu.'; return; }
      const files = [...input.files];
      if (files.length > 5 || files.some(f => f.size > 10 * 1024 * 1024)) { status.textContent = 'Tối đa 5 PDF, mỗi file 10 MB.'; return; }
      const form = new FormData(); files.forEach(file => form.append('files',file));
      button.disabled = true; status.textContent = 'Đang đọc PDF, vui lòng chờ...';
      try {
        const response = await originalFetch('http://localhost:5042/api/ai/documents',{method:'POST',headers:{Authorization:authorization},body:form});
        const result = await response.json();
        if (!response.ok) throw new Error(result.message || result.title || 'Không đọc được PDF.');
        status.textContent = result.message;
      } catch(error) { status.textContent = error.message; }
      finally { button.disabled = false; input.value = ''; }
    };
    row.append(agentLabel,button,input,status);
    heading.parentElement.append(row);
  }
  new MutationObserver(attach).observe(document.body,{childList:true,subtree:true});
  attach();
})();
