using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 주소 데이터를 저장하는 모델 클래스
    /// </summary>
    public class AddressData
    {
        /// <summary>
        /// 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 주소1
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 주소2
        /// </summary>
        public string Address2 { get; set; }

        /// <summary>
        /// 연락처
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 우편번호
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// 복사 매수 (기본값: 1)
        /// </summary>
        public int CopyCount { get; set; } = 1;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public AddressData()
        {
        }

        /// <summary>
        /// 매개변수가 있는 생성자
        /// </summary>
        public AddressData(string name, string address, string address2, string postalCode, string phoneNumber, int copyCount = 1)
        {
            Name = name ?? string.Empty;
            Address = address ?? string.Empty;
            Address2 = address2 ?? string.Empty;
            PostalCode = postalCode ?? string.Empty;
            PhoneNumber = phoneNumber ?? string.Empty;
            CopyCount = copyCount > 0 ? copyCount : 1;
        }

        /// <summary>
        /// 유효성 검사 - 최소한 하나의 필드가 채워져 있는지 확인
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) ||
                   !string.IsNullOrWhiteSpace(Address) ||
                   !string.IsNullOrWhiteSpace(Address2) ||
                   !string.IsNullOrWhiteSpace(PostalCode) ||
                   !string.IsNullOrWhiteSpace(PhoneNumber);
        }

        /// <summary>
        /// 문자열로 변환 (디버깅용)
        /// </summary>
        public override string ToString()
        {
            return $"이름: {Name}, 주소1: {Address}, 주소2: {Address2}, 우편번호: {PostalCode}, 연락처: {PhoneNumber}, 매수: {CopyCount}";
        }
    }
}

