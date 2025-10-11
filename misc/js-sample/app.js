const jsonata = require('jsonata');

const data = [
    { a: 'b'}, 
    { a: 'd'}, 
    { e: {
            a: 'f'
        }
    }
];

(async () => {
    const expression = jsonata('**.a');
    const result = await expression.evaluate(data);  // returns 24
    console.log(result);
})()

