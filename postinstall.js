// postinstall.js — SceneManager
// Creates required StreamingAssets sub-folder and optionally copies example files.

const fs   = require('fs');
const path = require('path');
const readline = require('readline');

const assetsDir   = path.resolve(__dirname, '../');
const examplesDir = path.resolve(__dirname, 'Examples');

const folders = [
  'StreamingAssets'
];

folders.forEach(folder => {
  const fullPath = path.join(assetsDir, folder);
  if (!fs.existsSync(fullPath)) {
    fs.mkdirSync(fullPath, { recursive: true });
    console.log(`Created folder: ${fullPath}`);
  } else {
    console.log(`Folder already exists: ${fullPath}`);
  }
});

function copyFileWithPrompt(src, dest, rl, cb) {
  if (fs.existsSync(dest)) {
    rl.question(`File ${dest} exists. Overwrite? (y/N): `, answer => {
      if (answer.trim().toLowerCase() === 'y') {
        fs.copyFileSync(src, dest);
        console.log(`Overwritten: ${dest}`);
      } else {
        console.log(`Skipped: ${dest}`);
      }
      cb();
    });
  } else {
    fs.copyFileSync(src, dest);
    console.log(`Copied: ${dest}`);
    cb();
  }
}

function getAllFiles(dir, files = []) {
  if (!fs.existsSync(dir)) return files;
  fs.readdirSync(dir).forEach(entry => {
    const full = path.join(dir, entry);
    if (fs.statSync(full).isDirectory()) getAllFiles(full, files);
    else files.push(full);
  });
  return files;
}

function copyTemplates() {
  if (!fs.existsSync(examplesDir)) {
    console.log('No Examples directory found. Skipping template copy.');
    return;
  }

  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  rl.question('Copy example files from SceneManager/Examples to your Assets folders? (y/N): ', answer => {
    if (answer.trim().toLowerCase() !== 'y') {
      console.log('Template copy skipped.');
      rl.close();
      return;
    }

    const allFiles = getAllFiles(examplesDir);
    let pending = allFiles.length;
    if (pending === 0) { rl.close(); return; }

    allFiles.forEach(src => {
      const rel  = path.relative(examplesDir, src);
      const dest = path.join(assetsDir, rel);
      const destDir = path.dirname(dest);
      if (!fs.existsSync(destDir)) fs.mkdirSync(destDir, { recursive: true });

      copyFileWithPrompt(src, dest, rl, () => {
        pending--;
        if (pending === 0) {
          console.log('Template copy complete.');
          rl.close();
        }
      });
    });
  });
}

copyTemplates();
