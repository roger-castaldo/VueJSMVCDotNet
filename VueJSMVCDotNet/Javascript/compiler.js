import { parse, compileScript, compileTemplate, compileStyle } from 'vue-compiler-sfc-esm-browser';

const exportedMark = 'export default {';
const templateExportMark = 'return (_openBlock(),';
const defineComponent = 'defineComponent';
const nameProperty = '__name';
const importRegex = /^\s*import\s*([^"']+)\s*from\s*("([^"]+)"|'([^']+)');?\s*$/m;
const asyncImportRegex = /^\s*const\s*(.+)\s*=\s*await\s+import\((.+)\);?\s*$/m;
const resolveComponentRegex = /^\s*const\s+([^\s]+)\s*=\s*_resolveComponent\("([^"]+)"\);?\s*$/mg;
const hoistedConstantRegex = /const\s+(_hoisted_\d+)\s*=\s*((?:.|\n)*?)(?=(?:\nconst\s+_hoisted_\d+\s*=|$))/;
const invalidNameChars = /(\s|-)/g;
const templateContextCacheRegex = /\b(_ctx|_cache)\./g;
const specialImportRegex = /^\s*const\s*(.+)\s*=\s*await\s+import\(`\$\{hosturl.origin\}(.+)`\);?\s*$/m;

const incrementMapName = (id) => {
    const chars = id.split('');
    const base = 26;
    let carry = 1;

    for (let i = chars.length - 1; i >= 0; i--) {
        if (carry === 0) break;

        let code = chars[i].charCodeAt(0) - 97; // 'a' = 97
        code += carry;

        if (code >= base) {
            chars[i] = 'a';
            carry = 1;
        } else {
            chars[i] = String.fromCharCode(97 + code);
            carry = 0;
        }
    }

    if (carry > 0) {
        chars.unshift('a');
    }

    return chars.join('');
};

const processImports = (fileindex,source, imports) => {
    for (let matches = importRegex.exec(source); matches != null; matches = importRegex.exec(source)) {
        const path = (matches[3] === undefined || matches[3] === null || matches[3] === '' ? matches[4] : matches[3]);
        if (imports[path] === undefined) {
            imports[path] = {
                variables: [],
                imports: [],
                isAsync: false
            };
        }
        if (matches[1].indexOf('{') === 0) {
            const imps = matches[1].split(',');
            for (let y = 0; y < imps.length; y++) {
                let imp = imps[y].trim();
                if (imp.startsWith('{')) {
                    imp = imp.substring(1);
                }
                if (imp.endsWith('}')) {
                    imp = imp.substring(0, imp.length - 1);
                }
                imp = imp.trim();
                if (imports[path].imports.indexOf(imp) < 0) {
                    imports[path].imports.push(imp);
                }
            }
        } else {
            if (imports[path].variables.indexOf(matches[1].trim()) < 0) {
                imports[path].variables.push(matches[1].trim());
            }
        }
        source = source.replace(matches[0], '');
    }
    for (let matches = asyncImportRegex.exec(source); matches != null; matches = asyncImportRegex.exec(source)) {
        const path = matches[2];
        if (imports[path] === undefined) {
            imports[path] = {
                variables: [],
                imports: [],
                isAsync: true
            };
        } else {
            imports[path].isAsync = true;
        }
        if (matches[1].indexOf('{') === 0) {
            const imps = matches[1].split(',');
            for (let y = 0; y < imps.length; y++) {
                let imp = imps[y].trim();
                if (imp.startsWith('{')) {
                    imp = imp.substring(1);
                }
                if (imp.endsWith('}')) {
                    imp = imp.substring(0, imp.length - 1);
                }
                imp = imp.trim();
                if (imports[path].imports.indexOf(imp) < 0) {
                    imports[path].imports.push(imp);
                }
            }
        } else {
            if (imports[path].variables.indexOf(matches[1].trim()) < 0) {
                imports[path].variables.push(matches[1].trim());
            }
        }
        source = source.replace(matches[0], '');
    }
    let tmp = source.substring(0, source.indexOf(exportedMark)).trim();
    let constants = [];
    for (let matches = hoistedConstantRegex.exec(tmp); matches != null; matches = hoistedConstantRegex.exec(tmp)) {
        const regex = new RegExp(`\\b${matches[1]}\\b`, 'g');
        tmp = tmp.replace(matches[0], '');
        source = source.replaceAll(regex, `_${fileindex}${matches[1]}`);
        constants.push(`const _${fileindex}${matches[1]} = ${matches[2].trim()};`);
    }

    source = source.replaceAll(resolveComponentRegex, (match, grp1, grp2) => `const ${grp1} = (${grp2}!==undefined ? ${grp2}.default : null)??${grp2}??${(grp2.toLowerCase() !== grp2 ? `(${grp2.toLowerCase()}!==undefined ? ${grp2.toLowerCase()}.default : null)??${grp2.toLowerCase()}??` : '')}_resolveComponent("${grp2}");`);

    return {
        source: source.substring(source.indexOf(exportedMark) + exportedMark.length).trim(),
        imports: imports,
        constants: constants
    };
};

const mergeImports = (encodedFiles) => {
    let curId = 'aa';
    let result = {
        vue: {
            variables: [],
            imports: [defineComponent],
            isAsync: false
        }
    };
    const currentImports = [defineComponent];
    for (let x = 0; x < encodedFiles.length; x++) {
        let imp = encodedFiles[x].imports;
        for (const prop in imp) {
            if (result[prop] === undefined) {
                let variables = imp[prop].variables.map(i => {
                    let checkValue = (i.indexOf(' as ') > 0 ? i.substring(i.indexOf(' as ') + 4) : i);
                    if (currentImports.indexOf(checkValue) < 0) {
                        currentImports.push(checkValue);
                    } else {
                        i = `${(i.indexOf(' as ') > 0 ? i.substring(0, i.indexOf(' as ')) : i)} as ${curId}`;
                        const regex = new RegExp(`\\b${checkValue}\\b`, 'g');
                        encodedFiles[x].scriptContent = encodedFiles[x].scriptContent.replaceAll(regex, curId);
                        curId = incrementMapName(curId);
                    }
                    return i;
                });
                let imports = imp[prop].imports.map(i => {
                    let checkValue = (i.indexOf(' as ') > 0 ? i.substring(i.indexOf(' as ') + 4) : i);
                    if (currentImports.indexOf(checkValue) < 0) {
                        currentImports.push(checkValue);
                    } else {
                        i = `${(i.indexOf(' as ') > 0 ? i.substring(0, i.indexOf(' as ')) : i)} as ${curId}`;
                        const regex = new RegExp(`\\b${checkValue}\\b`, 'g');
                        encodedFiles[x].scriptContent = encodedFiles[x].scriptContent.replaceAll(regex, curId);
                        curId = incrementMapName(curId);
                    }
                    return i;
                });
                result[prop] = {
                    variables: variables,
                    imports: imports,
                    isAsync: imp[prop].isAsync
                };
            } else {
                for (let y = 0; y < imp[prop].variables.length; y++) {
                    if (result[prop].variables.indexOf(imp[prop].variables[y]) < 0) {
                        result[prop].variables.push(imp[prop].variables[y]);
                    }
                }
                for (let y = 0; y < imp[prop].imports.length; y++) {
                    if (result[prop].imports.indexOf(imp[prop].imports[y]) < 0) {
                        result[prop].imports.push(imp[prop].imports[y]);
                    }
                }
                result[prop].isAsync |= imp[prop].isAsync;
            }
        }
    }
    return { encodedFiles: encodedFiles, imports: result };
};

const compileFiles = (files, preAsyncInject) => {
    const results = mergeImports(files.map((file, index) => {
        let importLines = [];

        let content = file.content;
        for (let matches = specialImportRegex.exec(content); matches != null; matches = specialImportRegex.exec(content)) {
            importLines.push(matches[0].trim());
            content = content.replace(matches[0].trim(), '');
        }

        const { descriptor } = parse(content, { id: file.id,filename:file.name });

        const scriptResults = compileScript(descriptor, {
            id: file.id,
            filename: file.name,
            isProd: true,
            sourceMap: false,
            inlineTemplate: true
        });

        let scriptContent = `${importLines.join('\n')}
${scriptResults.content}`;

        if (descriptor.scriptSetup === null) {

            const templateResults = compileTemplate({
                source: descriptor.template.content,
                filename: file.name,
                id: file.id,
                isProd: true
            });

            const templateImports = templateResults.code.substring(0, templateResults.code.indexOf(templateExportMark)).trim();
            let templateRenderCode = `render:(h) => { return ${templateResults.code.substring(templateResults.code.indexOf(templateExportMark) + templateExportMark.length).trim()}`
                .replaceAll(templateContextCacheRegex, 'h.');
            templateRenderCode = templateRenderCode.substring(0, templateRenderCode.length - 3);

            scriptContent = `${templateImports}
${scriptContent.replace(exportedMark, `${exportedMark}${templateRenderCode}},`)}`.trim();
            if (scriptContent.substring(scriptContent.length - 1) === ';') {
                scriptContent = scriptContent.substring(0, scriptContent.length-1);
            }
        }

        let styleCode = '';
        if (descriptor.styles.length > 0) {
            const { code: compiledStyleCode } = compileStyle({
                source: descriptor.styles[0].content,
                filename: file.name,
                id: file.id,
                scoped: descriptor.styles[0].scoped,
            });
            styleCode = compiledStyleCode;
        }

        let fixedScript = processImports(index, scriptContent, {});

        return {
            id: file.id,
            name: file.name,
            styleCode: styleCode,
            scriptContent: fixedScript.source.trim(),
            imports: fixedScript.imports,
            constants: fixedScript.constants
        };
    }));

    let resultCode = '';

    for (const prop in results.imports) {
        if (results.imports[prop] != undefined && !results.imports[prop].isAsync) {
            resultCode += 'import ';
            if (results.imports[prop].variables.length > 0) {
                resultCode += `${results.imports[prop].variables.join(', ')}${results.imports[prop].imports.length > 0 ? ',' : ''}`;
            }
            if (results.imports[prop].imports.length > 0) {
                resultCode += `{${results.imports[prop].imports.join(', ')}}`;
            }
            resultCode += ` from '${prop}';\n`;
        }
    }

    if (preAsyncInject !== undefined) {
        resultCode += `${preAsyncInject}\n`;
    }

    for (const prop in results.imports) {
        if (results.imports[prop] != undefined && results.imports[prop].isAsync) {
            resultCode += 'const ';
            if (results.imports[prop].variables.length > 0) {
                resultCode += `${results.imports[prop].variables.join(', ')}${results.imports[prop].imports.length > 0 ? ',' : ''}`;
            }
            if (results.imports[prop].imports.length > 0) {
                resultCode += `{${results.imports[prop].imports.join(', ')}}`;
            }
            resultCode += ` = await import(${prop});\n`;
        }
    }

    resultCode += `${results.encodedFiles.map(f => f.constants).flat().join('\n')}`;

    for (let x = 0; x < results.encodedFiles.length; x++) {
        resultCode += `
        ${results.encodedFiles[x].styleCode ? `(function(){
            let styleTag = document.createElement('style');
            styleTag.setAttribute('data-v-${results.encodedFiles[x].id}', '');
            styleTag.innerHTML = \`${results.encodedFiles[x].styleCode}\`;
            document.head.appendChild(styleTag);
        })()` : ''}
            var ${results.encodedFiles[x].id} = ${defineComponent}({
                ${nameProperty}: '${results.encodedFiles[x].id}',
                ${results.encodedFiles[x].scriptContent}
            );
        `;
    }

    if (results.encodedFiles.length === 1) {
        resultCode += `export default ${results.encodedFiles[0].id};`;
    } else {
        resultCode += `export {${results.encodedFiles.map(r => (r.name === `${r.id}.vue` ? r.id : `${r.id}, ${r.id} as ${r.name.substring(0, r.name.length - 4).replaceAll(invalidNameChars,'_')}`)).join(', ')}};`;
    }

    return resultCode;
};

export default compileFiles;