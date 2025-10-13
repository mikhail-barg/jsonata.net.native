const jsonata = require('jsonata');

const data = [
    { value: 3 }
];

(async () => {
    const expression = jsonata('[1, 2, 3][0]');
    const result = await expression.evaluate(data);  // returns 24
    console.log(result);
})()

