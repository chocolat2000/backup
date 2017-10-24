import React, { Component } from 'react';
import { formVMware, formWindows } from './Forms';

import { addServer } from '../../Data/Servers';

class Add extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  onChange = ({ target: { value, name } }) => {
    this.setState({ [name]: value });
  };

  handleSubmit = event => {
    event.preventDefault();

    addServer(this.state).then(newId => {
      const { props: { history } } = this;
      history.replace(`/servers/details/${newId}`);
    });
  };

  render() {
    let detailsForm = <div />;
    const formProperties = {
      withName: true,
      onChange: this.onChange,
      onSubmit: this.handleSubmit
    };
    switch (this.state.type) {
      case 'Windows':
        detailsForm = formWindows(this.state, formProperties);
        break;
      case 'VMware':
        detailsForm = formVMware(this.state, formProperties);
        break;
      default:
        detailsForm = <div />;
    }
    return (
      <section className="section">
        <div className="container">
          <div className="card">
            <div className="card-header">
              <div className="card-header-title">Add new server</div>
            </div>
            <div className="card-content">
              <div className="field is-horizontal">
                <div className="field-label is-normal">
                  <label className="label">Server type</label>
                </div>
                <div className="field-body">
                  <div className="field is-narrow">
                    <div className="control">
                      <div className="select is-fullwidth">
                        <select onChange={this.onChange} name="type">
                          <option>Choose ...</option>
                          <option>Windows</option>
                          <option>VMware</option>
                        </select>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              {detailsForm}
            </div>
          </div>
        </div>
      </section>
    );
  }
}

export default Add;
