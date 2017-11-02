import React from 'react';
import { Link, withRouter, matchPath } from 'react-router-dom';

const getNavClassName = (pathname, props) => {
  const baseClass = 'navbar-item is-tab';
  return matchPath(pathname, props) !== null
    ? `${baseClass} is-active`
    : baseClass;
};

const Navbar = ({ location: { pathname }, match: { path } }) => (
  <div className="navbar-tabs">
    <Link
      to={path}
      className={getNavClassName(pathname, { path, exact: true })}
    >
      Summary
    </Link>
    <Link
      to="/backups"
      className={getNavClassName(pathname, { path: '/backups' })}
    >
      Backups
    </Link>
    <Link
      to="/calendar"
      className={getNavClassName(pathname, { path: '/calendar' })}
    >
      Calendar
    </Link>
  </div>
);

export default withRouter(Navbar);
