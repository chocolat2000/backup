import React, { Component } from 'react';

const formData = {};

const onChange = ({ target: { name, value } }) => {
  formData[name] = value;
};

class Login extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  onSubmit = event => {
    event.preventDefault();
    if (formData.username && formData.password) {
      this.props.login(formData.username, formData.password);
    }
  };

  render() {
    const { error, inProgress } = this.props;
    return (
      <section className="section">
        <div className="container" style={{ maxWidth: '25rem' }}>
          <h1 className="title">Please log-in</h1>
          <form onSubmit={this.onSubmit}>
            <div className="field">
              <div className="control">
                <input
                  className="input"
                  type="text"
                  name="username"
                  onChange={onChange}
                  placeholder="Username"
                />
              </div>
            </div>
            <div className="field">
              <div className="control">
                <input
                  className="input"
                  type="password"
                  name="password"
                  onChange={onChange}
                  placeholder="Password"
                />
              </div>
              <p className="help is-danger">
                {error ? 'Nope, didn\'t work !' : ' '}
              </p>
            </div>
            <div className="field is-grouped">
              <div className="control">
                <button
                  className={
                    inProgress ? 'button is-link is-loading' : 'button is-link'
                  }
                >
                  Submit
                </button>
              </div>
            </div>
          </form>
        </div>
      </section>
    );
  }
}

export default Login;
