const jsonata = require('jsonata');

const data = [
    { value: 3 }
];

(async () => {
    const expression = jsonata('$.[value][]');
    const result = await expression.evaluate(data);  // returns 24
    console.log(result);
})()

