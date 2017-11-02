import React, { Component } from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import { isAuthenticated, registerAuthListener } from './Data/auth';
import Login from './Components/Login';
import Navbar from './Components/Navbar';
import ServersSummary from './Components/Servers/Summary';
import ServersNavbar from './Components/Servers/Navbar';
import ServersDetails from './Components/Servers/Details';
import ServersBackup from './Components/Backup/Server';
import ServersAdd from './Components/Servers/Add';

class EnsureLogin extends Component {
  constructor(props) {
    super(props);
    this.state = { authenticated: isAuthenticated() };
    this.unregister = registerAuthListener({
      authenticated: () => {
        this.setState({ authenticated: true });
      },
      unauthenticated: () => {
        this.setState({ authenticated: false });
      }
    });
  }

  componentWillUnmount() {
    this.unregister();
  }

  render() {
    return this.state.authenticated ? this.props.children : <Login />;
  }
}

const App = () => (
  <Router>
    <EnsureLogin>
      <section className="hero is-primary">
        <div className="hero-body">
          <div className="container">
            <h1 className="title">Hero title</h1>
            <h2 className="subtitle">Hero subtitle</h2>
          </div>
        </div>
        <div className="hero-foot">
          <Navbar />
        </div>
      </section>
      <nav className="navbar has-shadow">
        <div className="container">
          <Route path="/servers" component={ServersNavbar} />
        </div>
      </nav>
      <Route exact path="/servers" component={ServersSummary} />
      <Route exact path="/servers/details/:id" component={ServersDetails} />
      <Route exact path="/servers/backup/:id" component={ServersBackup} />
      <Route exact path="/servers/add" component={ServersAdd} />
    </EnsureLogin>
  </Router>
);

export default App;
