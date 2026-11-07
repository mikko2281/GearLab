"""Render the maintained Markdown audit to a standalone, offline HTML report."""
import html,re
from pathlib import Path

def inline(value):
    value=html.escape(value)
    value=re.sub(r'\[([^\]]+)\]\((https?://[^)]+)\)',r'<a href="\2">\1</a>',value)
    value=re.sub(r'\*\*(.+?)\*\*',r'<strong>\1</strong>',value)
    return re.sub(r'`([^`]+)`',r'<code>\1</code>',value)

if __name__=='__main__':
    path=Path(__file__).resolve().parents[1]/'docs/author-design-65/ИНЖЕНЕРНЫЙ_ОТЧЁТ.md'
    out=[];table=False;listing=False
    for line in path.read_text('utf-8-sig').splitlines():
        if line.startswith('|'):
            if not table:out.append('<div class="scroll"><table>');table=True
            if re.fullmatch(r'[| :\-]+',line):continue
            tag='th' if 'Объект Blender' in line else 'td'
            out.append('<tr>'+''.join(f'<{tag}>{inline(c.strip())}</{tag}>' for c in line.strip('|').split('|'))+'</tr>');continue
        if table:out.append('</table></div>');table=False
        if line.startswith('- '):
            if not listing:out.append('<ul>');listing=True
            out.append('<li>'+inline(line[2:])+'</li>');continue
        if listing:out.append('</ul>');listing=False
        if line.startswith('#'):
            n=len(line)-len(line.lstrip('#'));out.append(f'<h{n}>'+inline(line[n:].strip())+f'</h{n}>')
        elif line:out.append('<p>'+inline(line)+'</p>')
    if listing:out.append('</ul>')
    if table:out.append('</table></div>')
    style='body{background:#202328;color:#e4e8ec;font:16px/1.6 system-ui,sans-serif;margin:0 auto;padding:32px;max-width:1100px}h1,h2{color:#ffb366}h2{border-top:1px solid #454b53;padding-top:24px;margin-top:32px}a{color:#83bfff}code{background:#31363d;padding:2px 5px;border-radius:3px}table{border-collapse:collapse;font-size:14px;width:100%}td,th{border:1px solid #454b53;padding:9px;text-align:left;vertical-align:top}th{background:#343a43}.scroll{overflow:auto}li{margin-bottom:8px}strong{color:#fff}p{overflow-wrap:anywhere}@media print{body{background:white;color:#111}strong,h1,h2{color:#111}a{color:#245f96}table{font-size:10px}}'
    path.with_suffix('.html').write_text('<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Манипулятор — инженерная проверка</title><style>'+style+'</style><body>'+''.join(out)+'</body></html>',encoding='utf-8')
    print('Rendered standalone engineering audit')
