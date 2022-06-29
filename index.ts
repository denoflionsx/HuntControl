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

if (!fs.existsSync("./config.json")) {
    createConfig();
} else {
    build();
}

function createConfig() {
    fs.writeFileSync("./config.json", JSON.stringify({ outpath: "" } as Config, null, 2));
}

function build() {
    let inpath: string = path.resolve(`./bin/Release/netstandard2.1/${DLL_NAME}.dll`);
    let outpath: string = path.resolve((JSON.parse(fs.readFileSync("./config.json").toString()) as Config).outpath, `${DLL_NAME}.dll`);
    // Build the C#.
    console.log(child_process.execSync("dotnet build --configuration Release").toString());
    // Clean up all the extra crap VS throws into the release folder.
    let dir = path.parse(inpath).dir;
    fs.readdirSync(dir).forEach((file: string) => {
        let f = path.resolve(dir, file);
        if (fs.existsSync(f)) {
            if (path.parse(f).name !== DLL_NAME) {
                fs.unlinkSync(f);
            }
        }
    });
    // Copy file to game dir for testing.
    fs.copyFileSync(inpath, outpath);
    dist();
}

function generate_manifest() {
    let meta: any = JSON.parse(fs.readFileSync("./package.json").toString());
    let m = new Manifest(DLL_NAME, meta.version, meta.homepage, meta.description, meta.thunderstore.dependencies);
    return m.toBuffer();
}

function dist() {
    // Pack dll into zip file.
    let inpath: string = path.resolve(`./bin/Release/netstandard2.1/${DLL_NAME}.dll`);
    let zip = new AdmZip();
    zip.addFile(`${DLL_NAME}.dll`, fs.readFileSync(inpath));
    zip.addFile(`manifest.json`, generate_manifest());
    zip.addFile(`README.md`, fs.readFileSync("./README.md"));
    zip.addFile(`icon.png`, fs.readFileSync("./icon.png"));
    if (!fs.existsSync("./dist")) {
        fs.mkdirSync("./dist");
    }
    fs.writeFileSync(`./dist/${DLL_NAME}.zip`, zip.toBuffer());
}