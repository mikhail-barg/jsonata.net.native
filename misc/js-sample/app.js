const jsonata = require('jsonata');

const data = {
    "library": {
        "books": [
            { "a": "b" }
        ]
    }
};

(async () => {
    const expression = jsonata('library.(%%%)');
    const result = await expression.evaluate(data);  // returns 24
    console.log(result);
})()

