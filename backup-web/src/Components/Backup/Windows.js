import React, { Component } from 'react';
import './Windows.css';

class Windows extends Component {
  constructor(props) {
    super(props);

    this.state = {
      expandeditems: [],
      selecteditems: []
    };
  }

  startBackup = () => {
    const { backupNow, history, server: { id } } = this.props;
    const { selecteditems } = this.state;
    backupNow(selecteditems);
    history.push(`/servers/details/${id}`);
  };

  isIncluded = item => {
    return this.state.selecteditems.indexOf(item) > -1;
  };

  isExpanded = item => {
    return this.state.expandeditems.indexOf(item) > -1;
  };

  toggleInclude = item => () => {
    this.setState(({ selecteditems }) => {
      const fileIdx = selecteditems.indexOf(item);
      if (fileIdx === -1) selecteditems.push(item);
      else {
        selecteditems.splice(fileIdx, 1);
      }
      return { selecteditems };
    });
  };

  expandNode = name => () => {
    this.setState(({ expandeditems }) => {
      const fileIdx = expandeditems.indexOf(name);
      if (fileIdx === -1) {
        const { getContent } = this.props;
        expandeditems.push(name);
        getContent(name);
      } else {
        expandeditems.splice(fileIdx, 1);
      }
      return { expandeditems };
    });
  };

  renderFolder = (name, folder) => (
    <li key={name}>
      <a className="has-text-grey-darker" onClick={this.expandNode(name)}>
        <i
          className={`fa fa-folder${this.isExpanded(name) ? '-open' : ''}-o`}
        />{' '}
        {folder.name}
      </a>{' '}
      {this.isIncluded(name) ? (
        <i className="fa fa-check-circle has-text-success" />
      ) : (
        <a className="has-text-info" onClick={this.toggleInclude(name)}>
          <i className="fa fa-plus-circle" />
        </a>
      )}
      {this.isExpanded(name) && (
        <ul>
          {folder.folders &&
            Object.keys(folder.folders).map(f =>
              this.renderFolder(f, folder.folders[f])
            )}
          {folder.files &&
            Object.keys(folder.files).map(file => (
              <li key={file}>
                <i className="fa fa-file-o" /> {folder.files[file].name}{' '}
                {this.isIncluded(file) ? (
                  <i className="fa fa-check-circle has-text-success" />
                ) : (
                  <a
                    className="has-text-info"
                    onClick={this.toggleInclude(file)}
                  >
                    <i className="fa fa-plus-circle" />
                  </a>
                )}
              </li>
            ))}
        </ul>
      )}
    </li>
  );

  componentDidMount() {
    const { getContent } = this.props;
    getContent();
  }

  render() {
    const { name, serverfolders, isFetching } = this.props.server;
    const { selecteditems } = this.state;

    return (
      <section className="section">
        <div className="container" style={{ marginBottom: '1.5rem' }}>
          <h1 className="title">{name}</h1>
          <h2 className="subtitle">Chose items to backup</h2>
        </div>
        <div className="container">
          <div className="columns">
            <div
              className="column"
              style={{
                maxHeight: '400px',
                whiteSpace: 'nowrap',
                overflow: 'auto'
              }}
            >
              <div className="tree">
                <ul>
                  {!isFetching &&
                    serverfolders &&
                    Object.keys(serverfolders).map(f =>
                      this.renderFolder(f, serverfolders[f])
                    )}
                </ul>
              </div>
            </div>
            <div
              className="column"
              style={{
                maxHeight: '400px',
                whiteSpace: 'nowrap',
                overflow: 'auto'
              }}
            >
              <ul>
                {selecteditems && selecteditems.length > 0 ? (
                  selecteditems.map(item => (
                    <li key={item}>
                      <a
                        className="has-text-danger"
                        onClick={this.toggleInclude(item)}
                      >
                        <i className="fa fa-minus-circle is-danger" />
                      </a>{' '}
                      {item}
                    </li>
                  ))
                ) : (
                  <h3>Nothing selected ...</h3>
                )}
              </ul>
            </div>
          </div>
        </div>
        <div className="container" style={{ marginTop: '1.2rem' }}>
          <button className="button is-primary" onClick={this.startBackup}>
            Start Backup
          </button>
        </div>
      </section>
    );
  }
}

export default Windows;
