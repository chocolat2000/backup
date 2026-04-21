package proxy

import (
	"context"
	"fmt"
	"net/url"

	"github.com/vmware/govmomi"
	"github.com/vmware/govmomi/object"
	"github.com/vmware/govmomi/view"
	"github.com/vmware/govmomi/vim25/mo"
	"github.com/vmware/govmomi/vim25/types"
	"github.com/vmware/govmomi/vim25/methods"
)

// GovmomiProxy implements the VMwareProxy interface to interact with vCenter/ESXi servers.
type GovmomiProxy struct {
	client *govmomi.Client
	url    *url.URL
}

// NewGovmomiProxy initializes a new proxy object to connect to VMware.
func NewGovmomiProxy(serverIP string) (*GovmomiProxy, error) {
	u, err := url.Parse(fmt.Sprintf("https://%s/sdk", serverIP))
	if err != nil {
		return nil, err
	}

	return &GovmomiProxy{
		url: u,
	}, nil
}

// Login logs into the vCenter/ESXi server.
func (p *GovmomiProxy) Login(ctx context.Context, username, password string) error {
	p.url.User = url.UserPassword(username, password)

	// Create the govmomi client, ignoring certificates
	client, err := govmomi.NewClient(ctx, p.url, true)
	if err != nil {
		return fmt.Errorf("failed to login to vmware: %w", err)
	}

	p.client = client
	return nil
}

// Logout closes the vCenter/ESXi session.
func (p *GovmomiProxy) Logout(ctx context.Context) error {
	if p.client != nil {
		return p.client.Logout(ctx)
	}
	return nil
}

// GetVMs retrieves all Virtual Machines from the server.
// Returns an array of arrays representing [MoRef, Name].
func (p *GovmomiProxy) GetVMs(ctx context.Context) ([][]string, error) {
	if p.client == nil {
		return nil, fmt.Errorf("not logged in")
	}

	m := view.NewManager(p.client.Client)

	v, err := m.CreateContainerView(ctx, p.client.ServiceContent.RootFolder, []string{"VirtualMachine"}, true)
	if err != nil {
		return nil, err
	}
	defer v.Destroy(ctx)

	var vms []mo.VirtualMachine
	err = v.Retrieve(ctx, []string{"VirtualMachine"}, []string{"summary", "name"}, &vms)
	if err != nil {
		return nil, err
	}

	var result [][]string
	for _, vm := range vms {
		// Only list VMs that are templates or actual VMs based on summary config
		if !vm.Summary.Config.Template {
			result = append(result, []string{vm.Reference().Value, vm.Name})
		}
	}

	return result, nil
}

// ManagedEntity represents a basic structure for Folders and Pools in Arborescence.
type ManagedEntity struct {
	Name  string `json:"name"`
	MoRef string `json:"moref"`
	Type  string `json:"type"`
}

// GetFolders retrieves all folders from the datacenter.
func (p *GovmomiProxy) GetFolders(ctx context.Context) (interface{}, error) {
	if p.client == nil {
		return nil, fmt.Errorf("not logged in")
	}

	m := view.NewManager(p.client.Client)
	v, err := m.CreateContainerView(ctx, p.client.ServiceContent.RootFolder, []string{"Folder"}, true)
	if err != nil {
		return nil, err
	}
	defer v.Destroy(ctx)

	var folders []mo.Folder
	err = v.Retrieve(ctx, []string{"Folder"}, []string{"name"}, &folders)
	if err != nil {
		return nil, err
	}

	var result []ManagedEntity
	for _, f := range folders {
		result = append(result, ManagedEntity{
			Name:  f.Name,
			MoRef: f.Reference().Value,
			Type:  f.Reference().Type,
		})
	}

	return result, nil
}

// GetPools retrieves all resource pools.
func (p *GovmomiProxy) GetPools(ctx context.Context) (interface{}, error) {
	if p.client == nil {
		return nil, fmt.Errorf("not logged in")
	}

	m := view.NewManager(p.client.Client)
	v, err := m.CreateContainerView(ctx, p.client.ServiceContent.RootFolder, []string{"ResourcePool"}, true)
	if err != nil {
		return nil, err
	}
	defer v.Destroy(ctx)

	var pools []mo.ResourcePool
	err = v.Retrieve(ctx, []string{"ResourcePool"}, []string{"name"}, &pools)
	if err != nil {
		return nil, err
	}

	var result []ManagedEntity
	for _, rp := range pools {
		result = append(result, ManagedEntity{
			Name:  rp.Name,
			MoRef: rp.Reference().Value,
			Type:  rp.Reference().Type,
		})
	}

	return result, nil
}

// CreateSnapshot creates a snapshot of a virtual machine.
func (p *GovmomiProxy) CreateSnapshot(ctx context.Context, vmMoRef, name, description string, memory, quiesce bool) (string, error) {
	if p.client == nil {
		return "", fmt.Errorf("not logged in")
	}

	vmRef := types.ManagedObjectReference{Type: "VirtualMachine", Value: vmMoRef}
	vm := object.NewVirtualMachine(p.client.Client, vmRef)

	task, err := vm.CreateSnapshot(ctx, name, description, memory, quiesce)
	if err != nil {
		return "", err
	}

	err = task.Wait(ctx)
	if err != nil {
		return "", err
	}

	// Fetch task info to retrieve the snapshot reference
	taskInfo, err := task.WaitForResult(ctx, nil)
	if err != nil {
		return "", err
	}

	if snapRef, ok := taskInfo.Result.(types.ManagedObjectReference); ok {
		return snapRef.Value, nil
	}

	return "", fmt.Errorf("failed to retrieve snapshot reference")
}

// RemoveSnapshot deletes a given snapshot.
func (p *GovmomiProxy) RemoveSnapshot(ctx context.Context, snapMoRef string, removeChildren bool) error {
	if p.client == nil {
		return fmt.Errorf("not logged in")
	}

	snapRef := types.ManagedObjectReference{Type: "VirtualMachineSnapshot", Value: snapMoRef}

	req := types.RemoveSnapshot_Task{This: snapRef, RemoveChildren: removeChildren}

	res, err := methods.RemoveSnapshot_Task(ctx, p.client.Client, &req)
	if err != nil {
		return err
	}

	return object.NewTask(p.client.Client, res.Returnval).Wait(ctx)
}

// GetVMPowerState gets the current power state of the VM.
func (p *GovmomiProxy) GetVMPowerState(ctx context.Context, vmMoRef string) (string, error) {
	if p.client == nil {
		return "", fmt.Errorf("not logged in")
	}

	vmRef := types.ManagedObjectReference{Type: "VirtualMachine", Value: vmMoRef}
	var vm mo.VirtualMachine

	pc := p.client.PropertyCollector()
	err := pc.RetrieveOne(ctx, vmRef, []string{"runtime.powerState"}, &vm)
	if err != nil {
		return "", err
	}

	return string(vm.Runtime.PowerState), nil
}

// GetCBTState gets whether Changed Block Tracking is enabled and supported.
func (p *GovmomiProxy) GetCBTState(ctx context.Context, vmMoRef string) (enabled bool, supported bool, err error) {
	if p.client == nil {
		return false, false, fmt.Errorf("not logged in")
	}

	vmRef := types.ManagedObjectReference{Type: "VirtualMachine", Value: vmMoRef}
	var vm mo.VirtualMachine

	pc2 := p.client.PropertyCollector()
	err = pc2.RetrieveOne(ctx, vmRef, []string{"config.changeTrackingEnabled", "capability.changeTrackingSupported"}, &vm)
	if err != nil {
		return false, false, err
	}

	if vm.Config != nil && vm.Config.ChangeTrackingEnabled != nil {
		enabled = *vm.Config.ChangeTrackingEnabled
	}

	supported = vm.Capability.ChangeTrackingSupported

	return enabled, supported, nil
}
