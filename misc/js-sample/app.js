const jsonata = require('jsonata');

const data = [
    [
        { "bazz": "gotcha" }
    ]
];

(async () => {
    const expression = jsonata('bazz');
    const result = await expression.evaluate(data);  // returns 24
    console.log(result);
})()

