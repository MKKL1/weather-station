package domain

import "context"

type DeviceRepository interface {
	Get(ctx context.Context, deviceID string) (*Device, error)
	Save(ctx context.Context, device *Device) error
}
