const fs = require('fs');
const path = require('path');

const files = process.argv.slice(2);
const logRegex = /console\.log\s*\([\s\S]*?\)\s*;/g;

files.forEach((file) => {
  const filePath = path.resolve(file);
  if (fs.existsSync(filePath)) {
    const content = fs.readFileSync(filePath, 'utf-8');
    const updatedContent = content.replace(logRegex, '');
    if (content !== updatedContent) {
      fs.writeFileSync(filePath, updatedContent, 'utf-8');
    }
  }
});
