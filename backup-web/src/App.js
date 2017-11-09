import React from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import EnsureLogin from './Components/EnsureLogin';
import Navbar from './Components/Navbar';
import BackupsSummary from './Components/Backup';
import Servers from './Components/Servers';
import ServersNavbar from './Components/Servers/Navbar';

const App = () => (
  <EnsureLogin>
    <Router>
      <div>
        <section className="hero is-primary">
          <div className="hero-body">
            <div className="container">
              <h1 className="title">Backups</h1>
            </div>
          </div>
          <div className="hero-foot">
            <Route component={Navbar} />
          </div>
        </section>
        <nav className="navbar has-shadow">
          <div className="container">
            <Route path="/servers" component={ServersNavbar} />
          </div>
        </nav>
        <Route path="/backups" component={BackupsSummary} />
        <Route path="/servers" component={Servers} />
      </div>
    </Router>
  </EnsureLogin>
);

export default App;
