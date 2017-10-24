module.exports = {
    "env": {
        "browser": true,
        "node": true,
        "es6": true,
        "worker": true,
    },
    "parser": "babel-eslint",
    "extends": "eslint:recommended",
    /*
    "parserOptions": {
        "ecmaFeatures": {
            "experimentalObjectRestSpread": true,
            "jsx": true
        },
        "sourceType": "module"
    },
    */
    "plugins": [
        "react"
    ],
    "rules": {
        "indent": [
            "error",
            2, { "SwitchCase": 1 }
        ],
        "linebreak-style": [
            "error",
            "unix"
        ],
        "quotes": [
            "error",
            "single"
        ],
        "semi": [
            "error",
            "always"
        ],
        "react/jsx-uses-react": "error",
        "react/jsx-uses-vars": "error",
    }
};
