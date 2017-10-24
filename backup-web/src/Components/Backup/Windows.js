import React, { Component } from 'react';

import { backupNow, getDrives, getContent } from '../../Data/Servers';
import './Windows.css';

const splitName = name => {
  const splited = name.split('\\');
  if (splited.length === 2 && splited[1] === '') return [name];

  let prevVal = '';
  return splited.reduce((result, val, idx) => {
    if (val !== '') {
      if (prevVal === '') {
        prevVal = `${val}\\`;
      } else {
        if (idx === 1) {
          prevVal = `${prevVal}${val}`;
        } else {
          prevVal = `${prevVal}\\${val}`;
        }
      }
      result.push(prevVal);
    }
    return result;
  }, []);
};

class Windows extends Component {
  constructor(props) {
    super(props);

    this.state = {
      selecteditems: [],
      serverfolders: {}
    };
  }

  startBackup = () => {
    const { data: { server: { id } } } = this.props;
    const { selecteditems } = this.state;
    backupNow(id, selecteditems);
    this.props.history.push(`/servers/details/${id}`);
  };

  expandNode = name => () => {
    const { data: { server: { id } } } = this.props;
    const splited = splitName(name);

    this.setState(({ serverfolders }) => {
      let folder = splited.reduce((serverObject, path, index) => {
        return index === 0 ? serverObject[path] : serverObject.folders[path];
      }, serverfolders);
      folder.expanded = !folder.expanded;
      if (folder.expanded) {
        getContent(id, name).then(content => {
          if (!content) return;
          this.setState(({ serverfolders }) => {
            let folder = splited.reduce(
              (serverObject, path, index) =>
                index === 0 ? serverObject[path] : serverObject.folders[path],
              serverfolders
            );
            folder.folders = content.folders.reduce((result, folder) => {
              const split = folder.split('\\');
              result[folder] = { name: split[split.length - 1] };
              return result;
            }, {});
            folder.files = content.files.reduce((result, file) => {
              const split = file.split('\\');
              result[file] = { name: split[split.length - 1] };
              return result;
            }, {});
            return { serverfolders };
          });
        });
      }
      return { serverfolders };
    });
  };

  isIncluded = item => {
    return this.state.selecteditems.indexOf(item) > -1;
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

  renderFolder = (name, folder) => (
    <li key={name}>
      <a className="has-text-grey-darker" onClick={this.expandNode(name)}>
        <i className={`fa fa-folder${folder.expanded ? '-open' : ''}-o`} />{' '}
        {folder.name}
      </a>{' '}
      {this.isIncluded(name) ? (
        <i className="fa fa-check-circle has-text-success" />
      ) : (
        <a className="has-text-info" onClick={this.toggleInclude(name)}>
          <i className="fa fa-plus-circle" />
        </a>
      )}
      {folder.expanded && (
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
    const { data: { server: { id } } } = this.props;
    getDrives(id).then(drives => {
      if (!drives) return;
      this.setState({
        serverfolders: drives.reduce((result, drive) => {
          result[drive] = { name: drive };
          return result;
        }, {})
      });
    });
  }

  render() {
    const { data: { server: { name } } } = this.props;

    const { serverfolders, selecteditems } = this.state;

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
                  {Object.keys(serverfolders).map(f =>
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
