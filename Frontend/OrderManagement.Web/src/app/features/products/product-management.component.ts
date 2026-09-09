import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Product, SaveProduct } from '../../core/models/master-data.model';
import { AuthService } from '../../core/services/auth.service';
import { MasterDataApiService } from '../../core/services/master-data-api.service';

@Component({selector:'app-product-management',standalone:true,imports:[ReactiveFormsModule],templateUrl:'./product-management.component.html',styleUrl:'../shared/master-data.scss'})
export class ProductManagementComponent {
  private readonly api=inject(MasterDataApiService);private readonly fb=inject(FormBuilder);readonly auth=inject(AuthService);
  readonly items=signal<Product[]>([]);readonly dialogOpen=signal(false);readonly editingId=signal<number|null>(null);readonly saving=signal(false);readonly error=signal('');keyword='';
  readonly form=this.fb.nonNullable.group({code:['',Validators.required],name:['',Validators.required],unit:['Cái',Validators.required],price:[0,[Validators.required,Validators.min(0)]],stockQuantity:[0,[Validators.required,Validators.min(0)]],description:['']});
  constructor(){this.load()}
  load(){this.api.getProducts(this.keyword).subscribe({next:x=>this.items.set(x.map(item=>({
    ...item,
    orderedQuantity:item.orderedQuantity??0,
    availableQuantity:item.availableQuantity??item.stockQuantity,
  }))),error:e=>this.error.set(this.readError(e))})}
  openCreate(){this.editingId.set(null);this.form.reset({code:`HH${String(Date.now()).slice(-5)}`,unit:'Cái',price:0,stockQuantity:0});this.dialogOpen.set(true)}
  openEdit(x:Product){this.editingId.set(x.id);this.form.reset({code:x.code,name:x.name,unit:x.unit,price:x.price,stockQuantity:x.stockQuantity,description:x.description??''});this.dialogOpen.set(true)}
  save(){if(this.form.invalid){this.form.markAllAsTouched();return}const value=this.form.getRawValue() as SaveProduct;const id=this.editingId();const op=id?this.api.updateProduct(id,value):this.api.createProduct(value);this.saving.set(true);op.pipe(finalize(()=>this.saving.set(false))).subscribe({next:()=>{this.dialogOpen.set(false);this.load()},error:e=>this.error.set(this.readError(e))})}
  remove(x:Product){if(confirm(`Xóa hàng hóa ${x.code}?`))this.api.deleteProduct(x.id).subscribe({next:()=>this.load(),error:e=>this.error.set(this.readError(e))})}
  private readError(e:unknown){return e instanceof HttpErrorResponse?e.error?.message??'Không thể xử lý yêu cầu.':'Đã xảy ra lỗi.'}
}
