const jsonata = require('jsonata');
const fs = require("fs");

function FormatListIfExists(list, name, indent) {
    if (typeof list === 'undefined' || list === null)
    {
        return;
    }

    for(var ii = 0; ii < list.length; ii++) 
    {
        var element = list[ii];
        Format(element, "" + name + "[" + ii + "] ", indent);
    }
}

function FormatList2IfExists(list, name, indent) {
    if (typeof list === 'undefined' || list === null)
    {
        return;
    }

    for(var ii = 0; ii < list.length; ii++) 
    {
        var element = list[ii];
        for (var jj = 0; jj < element.length; jj++) {
            var subElement = element[jj]
            Format(subElement, "" + name + "[" + ii + "][" + jj + "] ", indent);
        }
    }
}

function Format(symbol, prefix, indent) {
    if (prefix != null) {
        process.stdout.write('\n');
    }
    for (i = 0; i < indent; ++i)
    {
        process.stdout.write('\t');
    }

    if (prefix != null)
    {
        process.stdout.write(prefix);
    }
    process.stdout.write("" + symbol.type + " ");
    process.stdout.write("bp=" + symbol.bp + " ");
    process.stdout.write("pos=" + symbol.position + " ");
    process.stdout.write("id=" + symbol.id + " ");
    if (typeof symbol.value !== 'undefined')
    {
        process.stdout.write("value=" + symbol.value + " ");
    }
    if (symbol.tuple)
    {
        process.stdout.write("tuple ");
    }
    if (symbol.consarray)
    {
        process.stdout.write("consarray ");
    }
    if (symbol.keepSingletonArray)
    {
        process.stdout.write("keepSingletonArray ");
    }
    if (typeof symbol.focus !== 'undefined')
    {
        process.stdout.write("focus=" + symbol.focus + " ");
    }
    if (typeof symbol.label !== 'undefined')
    {
        process.stdout.write("label=" + symbol.label + " ");
    }
    if (typeof symbol.index !== 'undefined')
    {
        process.stdout.write("index=" + symbol.index + " ");
    }
    if (typeof symbol.keepArray !== 'undefined')
    {
        process.stdout.write("keepArray=" + symbol.keepArray + " ");
    }    

    if (typeof symbol.ancestor !== 'undefined')
    {
        Format(symbol.ancestor, "ancestor: ", indent + 1);
    }

    if (typeof symbol.lhs != 'undefined')
    {
        Format(symbol.lhs, "lhs: ", indent + 1);
    }
    if (typeof symbol.rhs != 'undefined')
    {
        Format(symbol.rhs, "rhs: ", indent + 1);
    }

    if (typeof symbol.slot != 'undefined')
    {
        Format(symbol.slot, "slot: ", indent + 1);
    }
    if (typeof symbol.group != 'undefined')
    {
        Format(symbol.group, "group: ", indent + 1);
    }
    if (typeof symbol.expr != 'undefined')
    {
        Format(symbol.expr, "expr: ", indent + 1);
    }
    if (typeof symbol.nextFunction != 'undefined')
    {
        Format(symbol.nextFunction, "nextFunction: ", indent + 1);
    }
    if (typeof symbol.body != 'undefined')
    {
        Format(symbol.body, "body: ", indent + 1);
    }
    FormatListIfExists(symbol.steps, "steps", indent + 1);
    FormatListIfExists(symbol.stages, "stages", indent + 1);
    FormatListIfExists(symbol.predicate, "predicate", indent + 1);
    FormatListIfExists(symbol.arguments, "arguments", indent + 1);
    FormatListIfExists(symbol.expressions, "expressions", indent + 1);
    FormatListIfExists(symbol.seekingParent, "seekingParent", indent + 1);
    FormatListIfExists(symbol.terms, "terms", indent + 1);
    FormatListIfExists(symbol.rhsTerms, "rhsTerms", indent + 1);
    FormatList2IfExists(symbol.lhs, "lhsObject", indent + 1);
    FormatList2IfExists(symbol.rhs, "rhsObject", indent + 1);
}

const data = {
//    "library": {
//        "books": [
//            { "a": "b" }
//        ]
//    }
};

(async () => {
    const expression = jsonata(`$split("Hello", " ")`);
    //Format(expression.ast(), null, 0);
    //console.log('\n');
    const result = await expression.evaluate(data);  // returns 24
    //const data2 = JSON.parse(fs.readFileSync("../../jsonata-js/test/test-suite/datasets/library.json").toString());
    //const result = await expression.evaluate(data2);
    console.log(result);
})()

