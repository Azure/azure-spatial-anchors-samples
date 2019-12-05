// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import Foundation

class SensorStatusView: UIView {
    @IBOutlet var geoLocationStatusIcon: UIImageView!
    @IBOutlet var wifiStatusIcon: UIImageView!
    @IBOutlet var bluetoothStatusIcon: UIImageView!

    private var model: SensorStatusModel?

    override init(frame: CGRect) {
        super.init(frame: frame)
        commonInit()
    }

    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
        commonInit()
    }

    func setModel(_ model: SensorStatusModel?) {
        self.model = model
    }

    private func commonInit() {
        let ownType = type(of: self)
        let bundle = Bundle(for: ownType)
        let nib = bundle.loadNibNamed(String(describing: ownType), owner: self, options: nil)
        if let contentView = nib?.first as? UIView {
            self.frame.size = contentView.frame.size
            addSubview(contentView)
        }
    }

    public func update() {
        geoLocationStatusIcon.image = getStatusIcon(status: model?.geoLocationStatus)
        wifiStatusIcon.image = getStatusIcon(status: model?.wifiSignalStatus)
        bluetoothStatusIcon.image = getStatusIcon(status: model?.bluetoothSignalStatus)
    }

    private func getStatusIcon(status: SensorStatus?) -> UIImage? {
        switch status ?? .Indeterminate {
        case .Indeterminate: return UIImage(named: "gray-circle")
            case .Blocked: return UIImage(named: "red-circle")
            case .Unavailable: return UIImage(named: "orange-circle")
            case .Available: return UIImage(named: "green-circle")
        }
    }
}
