import child_process from 'child_process';
import fs from 'fs';
import path from 'path';

child_process.execSync("npm version minor --no-git-tag-version");

interface meta {
    name: string;
    version: string;
}

let m: meta = (JSON.parse(fs.readFileSync("./package.json").toString()) as meta);

let sln = fs.readFileSync(`./${m.name}.sln`).toString();
let p = sln.split("\n");
// Parse sln to find project files.
for (let i = 0; i < p.length; i++) {
    if (p[i].substring(0, "Project".length) === "Project") {
        let sub = p[i].split(",")[1].trim().replace(/['"]+/g, "");
        let proj = path.resolve(__dirname, sub);
        let data = fs.readFileSync(proj).toString().split("\n");
        // parse project files to bump version numbers.
        for (let j = 0; j < data.length; j++) {
            if (data[j].indexOf("<Version>") > -1) {
                // Find first > in the string.
                let index = data[j].indexOf(">");
                index++;
                // Find second tag start.
                let endex = data[j].indexOf("<", index);
                let first = data[j].substring(0, index);
                let second = data[j].substring(endex);
                // Rewrite string.
                data[j] = `${first}${m.version}${second}`;
            }
        }
        fs.writeFileSync(proj, Buffer.from(data.join("\n")));
    }
}
