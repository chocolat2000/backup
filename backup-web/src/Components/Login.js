import React, { Component } from 'react';
import { withRouter } from 'react-router-dom';
import { login } from '../Data/auth';

const formData = {};

const onChange = function({ target: { name, value } }) {
  formData[name] = value;
};

export default withRouter(
  class Details extends Component {
    constructor(props) {
      super(props);
      this.state = { error: false, loading: false };
    }

    onSubmit = (e) => {
      e.preventDefault();
      if (formData.username && formData.password) {
        this.setState({ error: false, loading: true });
        login(formData.username, formData.password)
          .then(() => {
            const { location, history: { replace } } = this.props;
            replace(location);
          })
          .catch(() => {
            this.setState({ error: true, loading: false });
          });
      }
    };

    onChange = e => {
      this.setState({ error: false });
      onChange(e);
    };

    render() {
      const { error, loading } = this.state;
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
                    onChange={this.onChange}
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
                    onChange={this.onChange}
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
                      loading ? 'button is-link is-loading' : 'button is-link'
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
);
