import React from 'react';
import { Link } from 'react-router-dom';

const formVMware = (
  { id, name, ip, username, password },
  { onChange, onSubmit, withName, withBackupNow }
) => (
  <form onSubmit={onSubmit}>
    {withName && (
      <div className="field is-horizontal">
        <div className="field-label is-normal">
          <label className="label">Name</label>
        </div>
        <div className="field-body">
          <div className="field">
            <p className="control">
              <input
                className="input"
                type="text"
                placeholder="Name"
                defaultValue={name}
                name="name"
                onChange={onChange}
              />
            </p>
          </div>
        </div>
      </div>
    )}
    <div className="field is-horizontal">
      <div className="field-label is-normal">
        <label className="label">vCenter</label>
      </div>
      <div className="field-body">
        <div className="field">
          <p className="control">
            <input
              className="input"
              type="text"
              placeholder="Ip or DNS"
              defaultValue={ip}
              name="ip"
              onChange={onChange}
            />
          </p>
        </div>
      </div>
    </div>
    <div className="field is-horizontal">
      <div className="field-label is-normal">
        <label className="label">Credentials</label>
      </div>
      <div className="field-body">
        <div className="field">
          <p className="control">
            <input
              className="input"
              type="text"
              placeholder="Username"
              defaultValue={username}
              name="username"
              onChange={onChange}
            />
          </p>
        </div>
        <div className="field">
          <p className="control">
            <input
              className="input"
              type="password"
              placeholder="Password"
              defaultValue={password}
              name="password"
              onChange={onChange}
            />
          </p>
        </div>
      </div>
    </div>
    <div className="field is-horizontal">
      <div className="field-label"> </div>
      <div className="field-body">
        <div className="field is-grouped">
          <div className="control">
            <button type="submit" className="button is-primary">
              Save
            </button>
          </div>
          {withBackupNow && (
            <div className="control">
              <Link to={`/servers/backup/${id}`} className="button">
                Backup Now
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  </form>
);

const formWindows = (
  { id, name, ip, username, password },
  { onChange, onSubmit, withName, withBackupNow }
) => (
  <form onSubmit={onSubmit}>
    {withName && (
      <div className="field is-horizontal">
        <div className="field-label is-normal">
          <label className="label">Name</label>
        </div>
        <div className="field-body">
          <div className="field">
            <p className="control">
              <input
                className="input"
                type="text"
                placeholder="Name"
                defaultValue={name}
                name="name"
                onChange={onChange}
              />
            </p>
          </div>
        </div>
      </div>
    )}
    <div className="field is-horizontal">
      <div className="field-label is-normal">
        <label className="label">Address</label>
      </div>
      <div className="field-body">
        <div className="field">
          <p className="control">
            <input
              className="input"
              type="text"
              placeholder="Ip or DNS"
              defaultValue={ip}
              name="ip"
              onChange={onChange}
            />
          </p>
        </div>
      </div>
    </div>
    <div className="field is-horizontal">
      <div className="field-label is-normal">
        <label className="label">Credentials</label>
      </div>
      <div className="field-body">
        <div className="field">
          <p className="control">
            <input
              className="input"
              type="text"
              placeholder="Username"
              defaultValue={username}
              name="username"
              onChange={onChange}
            />
          </p>
        </div>
        <div className="field">
          <p className="control">
            <input
              className="input"
              type="password"
              placeholder="Password"
              defaultValue={password}
              name="password"
              onChange={onChange}
            />
          </p>
        </div>
      </div>
    </div>
    <div className="field is-horizontal">
      <div className="field-label"> </div>
      <div className="field-body">
        <div className="field is-grouped">
          <div className="control">
            <button type="submit" className="button is-primary">
              Save
            </button>
          </div>
          {withBackupNow && (
            <div className="control">
              <Link to={`/servers/backup/${id}`} className="button">
                Backup Now
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  </form>
);

export { formVMware, formWindows };
