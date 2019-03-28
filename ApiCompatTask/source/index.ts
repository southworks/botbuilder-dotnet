import { join } from 'path'
import { getInput, setResult, TaskResult, getVariable } from 'azure-pipelines-task-lib/task';
import { execSync } from "child_process";
import { existsSync, writeFileSync, mkdirSync } from 'fs';

const run = (): void => {
    // Create ApiCompat path
    const ApiCompatPath = join(__dirname, 'ApiCompat', 'Microsoft.DotNet.ApiCompat.exe');
    
    // Show the ApiCompat version
    console.log(execSync(`"${ ApiCompatPath }" --version`).toString());
    
    // Get the binaries to compare and create the command to run
    const inputFiles: string = getInputFiles();
    const command = `"${ApiCompatPath}" "${inputFiles}" --impl-dirs "${getInput('implFolder')}" ${getOptions()}`;

    // Run the ApiCompat command
    console.log(command);
    runCommand(command);
}

const getInputFiles = (): string => {
    const filesName: string[] = [];

    getInput('contractsFileName').split(' ').forEach(file => {
        const fullFilePath: string = join(getInput('contractsRootFolder'), file);
        if (existsSync(fullFilePath)) {
            filesName.push(fullFilePath);
        }
    });

    return filesName.join(',');
}

const runCommand = (command: string): void => {
    const result = execSync(command).toString();
    const issuesCount: number = getTotalIssues(result, result.indexOf("Total Issues"));
    const body: string = getBody(result, result.indexOf("Total Issues"));
    const compatResult: TaskResult = getCompattibilityResult(issuesCount);
    const colorCode: string = getColorCode(issuesCount);
    const resultText = issuesCount != 0 ?
        `There were ${ issuesCount } differences between the assemblies` :
        `No differences were found between the assemblies` ;
    
    console.log(body + colorCode + 'Total Issues : ' + issuesCount);
    writeResult(body, issuesCount);
    setResult(compatResult, resultText);
}

const getOptions = (): string => {
    var command = getInput('resolveFx') === 'true' ? ' --resolve-fx' : '';
    command += getInput('warnOnIncorrectVersion') === 'true' ? ' --warn-on-incorrect-version' : '';
    command += getInput('warnOnMissingAssemblies') === 'true' ? ' --warn-on-missing-assemblies' : '';

    return command;
}

const getCompattibilityResult = (totalIssues: number): TaskResult => {
    return totalIssues === 0
        ? TaskResult.Succeeded
        : getInput('failOnIssue') === 'true'
            ? TaskResult.Failed
            : TaskResult.SucceededWithIssues;
}

const getColorCode = (totalIssues: number): string => {
    return totalIssues === 0
        ? "\x1b[32m"
        : getInput('failOnIssue') === 'true'
            ? "\x1b[31m"
            : "\x1b[33m";
}

const getTotalIssues = (message: string, indexOfResult: number): number => {
    return parseInt(message.substring(indexOfResult).split(':')[1].trim(), 10);
}

const getBody = (message: string, indexOfResult: number): string => {
    return message.substring(0, indexOfResult - 1);
}

const writeResult = (body: string, issues: number): void => {
    var test: any;
    
    if (issues === 0) {
        test = {
            issues: 0,
            body: `No issues found in ${ getInput('contractsFileName') }`
        }
    } else {
        test = {
            issues: issues,
            body: body
        }
    }

    const fileName: string = getInput('outputFilename');
    const directory: string = getInput("outputFolder");
    console.log('Filename: ' + fileName);
    console.log('Directory: ' + directory);
    if (!existsSync(directory)) {
        mkdirSync(directory, { recursive: true });
    }
    
    writeFileSync(`${join(directory, fileName)}.result.json`, JSON.stringify(test, null, 2) );
}

run();
