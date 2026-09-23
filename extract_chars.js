const fs = require('fs');
const path = require('path');

const dir = 'Assets/Resources/CardEntityList';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.asset'));

const unescape = (v) => v
  .replace(/\\u([0-9A-Fa-f]{4})/g, (_, h) => String.fromCharCode(parseInt(h, 16)))
  .replace(/\\n/g, '\n')
  .replace(/\\t/g, '\t')
  .replace(/\\"/g, '"');

const descs = [];
for (const f of files) {
  const text = fs.readFileSync(path.join(dir, f), 'utf8');
  for (const line of text.split(/\r?\n/)) {
    const m = line.match(/^\s*description:\s*(.*)$/);
    if (!m) continue;
    let v = m[1].trim();
    if (v.startsWith('"') && v.endsWith('"')) v = v.slice(1, -1);
    descs.push({ file: f, text: unescape(v) });
  }
}

const set = new Set();
for (const d of descs) {
  for (const ch of d.text) {
    if (ch === '\n' || ch === '\r') continue;
    set.add(ch);
  }
}
const chars = [...set].sort((a, b) => a.codePointAt(0) - b.codePointAt(0));

fs.writeFileSync('description_list.txt', descs.map(d => d.file + '\t' + d.text.replace(/\n/g, '\\n')).join('\n') + '\n', 'utf8');
fs.writeFileSync('custom_characters.txt', chars.join('') + '\n', 'utf8');

console.log('assets:', files.length, '/ descriptions:', descs.length, '/ unique chars:', chars.length);
console.log('---');
console.log(chars.join(''));
