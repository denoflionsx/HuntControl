import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import AdmZip from 'adm-zip';

const DLL_NAME: string = "HuntControl"

interface Config {
    outpath: string;
}

interface IManifest {
    name: string,
    version_number: string,
    website_url: string,
    description: string,
    dependencies: string[];
}

class Manifest implements IManifest {
    name: string;
    version_number: string;
    website_url: string;
    description: string;
    dependencies: string[];

    constructor(name: string, version_number: string, website_url: string, description: string, dependencies: string[]) {
        this.name = name;
        this.version_number = version_number;
        this.website_url = website_url;
        this.description = description;
        this.dependencies = dependencies;
    }

    toBuffer() {
        return Buffer.from(JSON.stringify(this, null, 2));
    }

}

function getAllFiles(dirPath: string, arrayOfFiles: Array<string>, ext: string = "*") {
    let files = fs.readdirSync(dirPath);

    arrayOfFiles = arrayOfFiles || [];

    files.forEach((file) => {
        if (fs.statSync(dirPath + "/" + file).isDirectory()) {
            arrayOfFiles = getAllFiles(dirPath + "/" + file, arrayOfFiles, ext);
        }
        else {
            if (path.parse(file).ext === ext || ext === "*") {
                arrayOfFiles.push(path.join(dirPath, "/", file));
            }
        }
    });

    return arrayOfFiles;
}

if (!fs.existsSync("./config.json")) {
    createConfig();
} else {
    build();
}

function createConfig() {
    fs.writeFileSync("./config.json", JSON.stringify({ outpath: "" } as Config, null, 2));
}

function build() {
    let inpath: string = path.resolve(`./subprojects/HuntControl/bin/Release`);
    let outpath: string = path.resolve((JSON.parse(fs.readFileSync("./config.json").toString()) as Config).outpath);
    // Build the C#.
    console.log(child_process.execSync("dotnet build --configuration Release").toString());
    // Clean up all the extra crap VS throws into the release folder.
    let files = getAllFiles(inpath, [], ".dll");
    let dlls: string[] = [];
    files.forEach((file: string) => {
        if (path.parse(file).base.indexOf(DLL_NAME) === -1) {
            //fs.unlinkSync(file);
        } else {
            dlls.push(file);
        }
    });
    // Copy file to game dir for testing.
    dlls.forEach((dll: string) => {
        console.log(dll);
        fs.copyFileSync(dll, path.resolve(outpath, path.parse(dll).base));
    });
    dist(dlls);
}

function generate_manifest() {
    let meta: any = JSON.parse(fs.readFileSync("./package.json").toString());
    let m = new Manifest(DLL_NAME, meta.version, meta.homepage, meta.description, meta.thunderstore.dependencies);
    return m.toBuffer();
}

function dist(dlls: string[]) {
    // Pack dll into zip file.
    let zip = new AdmZip();
    dlls.forEach((dll: string) => {
        zip.addFile(path.parse(dll).base, fs.readFileSync(dll));
    });
    zip.addFile(`manifest.json`, generate_manifest());
    zip.addFile(`README.md`, fs.readFileSync("./README.md"));
    zip.addFile(`icon.png`, fs.readFileSync("./icon.png"));
    if (!fs.existsSync("./dist")) {
        fs.mkdirSync("./dist");
    }
    fs.writeFileSync(`./dist/${DLL_NAME}.zip`, zip.toBuffer());
}