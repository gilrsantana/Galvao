import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-admin-button',
  standalone: true,
  template: `
    <button 
      [type]="type"
      [disabled]="disabled"
      (click)="btnClick.emit($event)" 
      [class]="getButtonClass()"
      [style]="getButtonStyle()">
      <ng-content></ng-content>
    </button>
  `,
  host: {
    '[style.display]': "fullWidth ? 'block' : 'inline-block'",
    '[style.width]': "fullWidth ? '100%' : 'auto'"
  }
})
export class AdminButtonComponent {
  @Input() variant: 'primary' | 'secondary' | 'gold' | 'danger' = 'gold';
  @Input() size: 'xs' | 'sm' | 'md' | 'lg' = 'sm';
  @Input() disabled = false;
  @Input() type: 'button' | 'submit' = 'button';
  @Input() fullWidth = false;
  @Input() widthClass = '';

  @Output() btnClick = new EventEmitter<MouseEvent>();

  getButtonClass(): string {
    const classes = ['btn'];
    
    // Add variant class
    classes.push(`btn-${this.variant}`);
    
    // Add size class
    if (this.size === 'sm' || this.size === 'xs') {
      classes.push('btn-sm');
    }
    
    // Add width class
    if (this.fullWidth) {
      classes.push('w-full');
    } else if (this.widthClass) {
      classes.push(this.widthClass);
    }
    
    return classes.join(' ');
  }

  getButtonStyle(): string {
    if (this.size === 'xs') {
      return 'padding: 6px 12px; font-size: 11px;';
    }
    return '';
  }
}

