import React, { Component } from 'react';

class VMware extends Component {
  constructor(props) {
    super(props);

    this.state = {
      selectedvms: []
    };
  }

  handleChooseVM = ({ target }) => {
    const { name, checked } = target;

    this.setState(({ selectedvms }) => {
      const index = selectedvms.indexOf(name);
      if (checked) {
        if (index < 0) selectedvms.push(name);
      } else {
        if (index > -1) selectedvms.splice(index, 1);
      }
      return { selectedvms };
    });
  };

  startBackup = () => {
    const { selectedvms } = this.state;
    this.props.backupNow(selectedvms);
  };

  render() {
    const { serverDetails, server: { name, vms } } = this.props;
    const vmsArray = Object.entries(vms);

    let vmSplices = [];
    for (var i = 0; i < vmsArray.length / 10; i++) {
      vmSplices.push(vmsArray.slice(i * 10, i * 10 + 10));
    }

    return (
      <section className="section">
        <div className="container" style={{ marginBottom: '1.5rem' }}>
          <h1 className="title">{name}</h1>
          <h2 className="subtitle">Chose vms to backup</h2>
        </div>
        <div className="container">
          <div className="columns">
            {vmSplices.map((vmsBucket, idx) => (
              <div className="column" key={idx}>
                {vmsBucket.map(([moref, name]) => (
                  <div className="field is-marginless" key={moref}>
                    <div className="control">
                      <label className="checkbox is-small">
                        <input
                          className="is-small"
                          type="checkbox"
                          name={moref}
                          onChange={this.handleChooseVM}
                        />{' '}
                        {name}
                      </label>
                    </div>
                  </div>
                ))}
              </div>
            ))}
          </div>
        </div>
        <div className="container" style={{ marginTop: '1.2rem' }}>
          <div className="field is-grouped">
            <div className="control">
              <button className="button is-primary" onClick={this.startBackup}>
                Start Backup
              </button>
            </div>
            <div className="control">
              <button className="button" onClick={() => serverDetails(true)}>
                <span className="icon">
                  <i className="fa fa-refresh" aria-hidden="true" />
                </span>
                <span>Refresh VMs list</span>
              </button>
            </div>
          </div>
        </div>
      </section>
    );
  }
}

export default VMware;
